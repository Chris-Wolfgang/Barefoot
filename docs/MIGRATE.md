# bf-maint

This document is the operator contract for `bf-maint` (`Wolfgang.Barefoot.Maintenance`): the verbs `migrate`,
`verify-schema`, `grant-roles` and `assign-owner`. It restates ADR 0001 §4–§5 (the migrate rules, the
firewall rule, session hygiene), ADR 0007 (the host and the shared `ExitCode` enum), ADR 0014 (exit code
11), ADR 0004 §8 (`assign-owner`) and ADR 0005 (database identities). It adds no decision.

Nothing described here is built yet. `bf-maint` arrives with build phase 2 (DESIGN §13); the Azure database,
the firewall rule and the managed identity arrive with phase 6. Points the ADRs leave open are listed in
[the gaps note](notes/2026-11-20-convention-gaps.md) and are marked here as *(open: gap M-n)*.

## What it is

- `bf-maint` is the one way the schema is created, upgraded, scripted or rolled back. Nothing else migrates.
  The API never changes the schema: its managed identity holds `db_datareader` + `db_datawriter` only.
- `bf-maint` is the **only** client that connects to the database directly. Every other process reaches the
  database through the API (ADR 0002).
- It depends on `Data` and `Core`. It never references `ApiClient` or `Obe`.
- Nothing a user would do belongs in it.

**Enforced by:** the project graph and the architecture tests (`Maintenance` ⟂ `ApiClient`, `Obe`; arrive
with phase 0).

## Identity and connection

- It connects with a **DDL identity**: the operator's Entra user when run locally, or the pipeline job's
  managed identity in the release pipeline.
- `Authentication=Active Directory Default` is on the connection string. There is no SQL password anywhere.
- It runs from the operator's machine or from the release pipeline.
- It works **after warm-up**: it waits for the serverless database to resume, so a migration never starts
  against a sleeping database. It uses the same connection options as the API (`Connect Timeout` 90 s, the
  retrying execution strategy; ADR 0001 §1).
- It connects, works and disconnects.

## Verbs

| Verb | Does |
|---|---|
| `bf-maint migrate` | applies every pending migration, up to the latest; runs `grant-roles` as part of the same verb |
| `bf-maint migrate --to <target>` | moves up **or** down to `<target>` and states the direction it took |
| `bf-maint migrate --status` | reports the current schema version against the one this build expects; its exit code says which way they differ (below) |
| `bf-maint migrate --script [--from <m>] [--to <m>] --output <file>` | renders the SQL offline, for review |
| `bf-maint verify-schema` | *(open: gap M-1 — the ADRs name the verb and do not describe it)* |
| `bf-maint grant-roles` | creates the contained database users and roles; idempotent; also runs as part of `migrate` |
| `bf-maint assign-owner <oid>` | moves every row owned by the "Unassigned" user to the user `<oid>`; run once |

### Options of `migrate`

| Option | Meaning |
|---|---|
| `--to <id\|name\|prefix\|0>` | the target migration: its id, its name, a prefix, or `0` for an empty schema. Default: latest |
| `--status` | report only; changes nothing |
| `--script` | write SQL instead of applying it |
| `--from <m>` | with `--script`: start of the range |
| `--output <file>` | with `--script`: where the SQL is written |
| `--confirm-data-loss` | required for a downgrade that would lose data |

The tool is **declarative**. You name a target, never a direction.

### `grant-roles`

- Creates the contained users and roles the application needs: the API's managed identity gets
  `db_datareader` + `db_datawriter`.
- Idempotent: running it again changes nothing.
- Runs as part of `migrate`, so a fresh database is usable by the API after one command.

### `assign-owner <oid>`

- Before the application is connected to Entra there is one developer and no real object id. Local runs act
  as the seeded "Unassigned" user (`Guid.Empty`), who owns everything created in that time.
- Once sign-in works, `bf-maint assign-owner <oid>` moves every row that user owns to the user `<oid>`.
- `<oid>` must be a user who has signed in once (their `AppUser` row exists).
- It is run once. Afterwards the "Unassigned" row stays seeded and owns nothing.

## Exit codes

Exit codes come from the one `ExitCode` enum in `Core`, compiled into all three binaries (`bf-scraper`,
`bf`, `bf-maint`). One numbering; no code means two things.

| Code | Name | Meaning | Used by |
|---|---|---|---|
| 0 | `Ok` | done | all |
| 1 | `Failed` | the verb failed (a migration failed, an unhandled exception) | all |
| 2 | `Partial` | finished with warnings; some items did not complete | `bf-scraper`, `bf` |
| 3 | `BreakerTripped` | the scraper stopped the run; resume later | `bf-scraper` |
| 4 | `Usage` | bad arguments or configuration | all |
| 5 | `VersionMismatch` | the API is too old or too new for this binary (ADR 0010 §8) | `bf-scraper`, `bf` |
| 6 | `DataLossNotConfirmed` | a downgrade would lose data and `--confirm-data-loss` was not given | `bf-maint` |
| 7 | `ApiUnavailable` | the API could not be reached within the retry budget | `bf-scraper`, `bf` |
| 8 | `DatabaseUnavailable` | the database could not be reached within the retry budget | all |
| 9 | `QuotaExhausted` | the monthly free allowance is used up (ADR 0001 §3) | all |
| 10 | `Refused` | the tool will not act (a schema newer than the build, ADR 0001 §4) | `bf-maint` |
| 11 | `MissingMigration` | `migrate --status` found the database behind this build (ADR 0014) | `bf-maint` |

`bf-maint` uses 0, 1, 4, 6, 8, 9, 10 and 11.

### What `migrate --status` exits with

| The database is | Exit code |
|---|---|
| at the version this build expects | `Ok` (0) |
| behind this build: a migration is pending | `MissingMigration` (11) |
| newer than this build | `Refused` (10) |

`--status` changes nothing in any of the three. Only `--status` exits 11: `bf-maint migrate` on a database
that is behind applies the pending migrations and exits 0 (ADR 0014).

`docs/EXIT-CODES.md` is generated from the enum.

**Enforced by:** the generated-file test that fails when the committed `docs/EXIT-CODES.md` differs from the
enum (the enum is in `Core`, phase 1; `bf-maint` is its first user, phase 2); the migrate-tool tests, run
in-process with injected writers, one per exit code (phase 2).

## Rules

### The database must exist

The tool never creates a database. A mistyped name must not create a stray database. The operator creates
the database (Azure, phase 6) and `bf-maint` fills it.

**Enforced by:** the migrate-tool tests (phase 2) *(open: gap M-4 — which exit code)*.

### A schema newer than the build is never touched

When the database holds migrations this build does not ship, the tool applies nothing, in either direction,
and exits `Refused` (10). Deploy the matching build, or restore a backup.

The API does not refuse to start on a newer schema. It reports the readiness state `NewerSchema` and answers
its data endpoints with `503 database.newer_schema`. It reads and writes nothing.

**Enforced by:** the migrate-tool tests (phase 2); the readiness state-machine tests (phase 2).

### A downgrade that would lose data needs confirmation

A downgrade is screened for data loss. These count:

- a dropped table;
- a dropped column;
- a narrowing column change;
- raw SQL (the tool does not inspect it, so it treats it as data-losing).

Without `--confirm-data-loss` the tool exits `DataLossNotConfirmed` (6) and changes nothing. A downgrade
script carries a `-- Downgrade` header naming each such step.

**Enforced by:** the migrate-tool tests over the data-loss screen (phase 2).

### Every migration has a working `Down`

CI applies up → down → up against SQL Server. **Backup restore is the primary rollback**; `Down` is for the
pipeline and for development.

**Enforced by:** the CI step that applies the migration up → down → up on SQL Server (phase 2).

### A schema behind the build is a state, not a crash

The API always starts. When migrations are pending it reports the readiness state `MissingMigration`, names
them ("Missing migration: `<names>`. Run `bf-maint migrate`."), and answers its data endpoints with
`503 database.missing_migration`. The web UI renders this full-page.

`GET /system/schema` answers `{current, expected, upToDate}` from the same code path `bf-maint` uses, so the
API and the tool cannot disagree about the schema version.

**Enforced by:** the readiness state-machine tests (phase 2); the API contract tests (phase 4).

### A sleeping or exhausted database

| Situation | What the tool does |
|---|---|
| the database is paused | waits for the resume inside the retry budget, then works |
| the database cannot be reached within the retry budget | exits `DatabaseUnavailable` (8) |
| the monthly free allowance is used up | prints the quota sentence with the reset instant (00:00 UTC on the first day of the next month) and exits `QuotaExhausted` (9) |

## Before you run it: the firewall rule

- The public IP of the machine that runs `bf-maint` is allow-listed on the Azure SQL server.
- That machine is the only one that needs a rule. The scraper and `bf` reach the API over HTTPS.
- Home IPs change. Run `az sql server firewall-rule update` before a `migrate`, or re-add the rule when the
  tool cannot connect.

**Enforced by:** operator checklist line in this document; the tool reports `DatabaseUnavailable` (8) when
the rule is stale (phase 2; the rule itself is created in phase 6).

## No client stays connected

A serverless database pauses after 15 minutes without activity. There is **no manual pause**; inactivity is
the only trigger. An open session holds the database awake and spends the monthly free allowance.

- Do not leave SSMS Object Explorer, Azure Data Studio or a `sqlcmd` session connected.
- `bf-maint` connects, works and disconnects. No verb stays resident.
- The Admin dashboard shows the database `status` and `free_amount_remaining`. `Online` at an odd hour means
  a session is holding the database awake.

**Enforced by:** operator checklist line in this document; the Admin dashboard's Budget & health card
(arrives with phase 7; the Azure Monitor figures follow the managed-identity wiring of phase 6).

## Output and logging

- **stdout** carries only the verb's own output, culture-invariant, so it stays parseable.
- Logs go to **stderr and a rolling file** under the user's local application data.
- An unhandled exception is one line on stderr, the stack trace in the file only, and exit code `Failed` (1).

**Enforced by:** the migrate-tool tests, which capture the injected stdout and stderr writers (phase 2);
review checklist line: "no host writes a log line with `Console.Write`" (pr-gate skill, phase 0).

## Examples

```bash
bf-maint migrate --status
bf-maint migrate
bf-maint migrate --script --output upgrade.sql
bf-maint migrate --to 0 --confirm-data-loss
bf-maint grant-roles
bf-maint assign-owner <oid>
```

## Operator checklist for an upgrade

1. Update the firewall rule if your IP changed.
2. `bf-maint migrate --status` — read current against expected.
3. `bf-maint migrate` — applies the pending migrations and grants roles.
4. Deploy the matching API build. Until then the API reports `NewerSchema` and serves no data.
5. Disconnect every other tool from the database.

# bf-maint

This document is the operator contract for `bf-maint` (`Wolfgang.Barefoot.Maintenance`): the verbs `migrate`,
`verify-schema`, `grant-roles` and `assign-owner`. It restates ADR 0001 §4–§5 (the migrate rules, the
firewall rule, session hygiene), ADR 0007 (the host and the shared `ExitCode` enum), ADR 0014 (exit code
11), ADR 0015 (the connection, a missing database, `--script`), ADR 0016 (the other points of `migrate`, the
other three verbs, logging), ADR 0004 §8 (`assign-owner`) and ADR 0005 (database identities). It adds no decision.

Of the verbs, `bf-maint migrate` is built with all its options: `--to`, `--confirm-data-loss`, `--status`,
and `--script` with `--from`, `--to` and `--output`. `migrate` does not run `grant-roles` yet, and the file
log is not built yet. The rest of `bf-maint` arrives with build phase 2 (DESIGN §13); the Azure database,
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
- The connection string is the configuration key `ConnectionStrings:Barefoot`: the environment variable
  `ConnectionStrings__Barefoot`, or a user secret on a developer's machine. There is no command-line flag
  for it. Without it the tool exits `Usage` (4) and opens nothing (ADR 0015).

**Enforced by:** the migrate-tool tests (`MigrateStatusTests`, `SqlServerDatabaseTests`).

## Verbs

| Verb | Does |
|---|---|
| `bf-maint migrate` | applies every pending migration, up to the latest; runs `grant-roles` as part of the same verb |
| `bf-maint migrate --to <target>` | moves up **or** down to `<target>` and states the direction it took |
| `bf-maint migrate --status` | reports the current schema version against the one this build expects; its exit code says which way they differ (below) |
| `bf-maint migrate --script [--from <m>] [--to <m>] --output <file>` | renders the SQL offline, for review |
| `bf-maint verify-schema` | compares the live schema with the one this build expects and reports each difference; changes nothing |
| `bf-maint grant-roles` | creates the contained database users and roles; idempotent; also runs as part of `migrate` |
| `bf-maint assign-owner <oid>` | moves every row owned by the "Unassigned" user to the user `<oid>`; run once |

### Options of `migrate`

| Option | Meaning |
|---|---|
| `--to <id\|name\|prefix\|0>` | the target migration: its id, its name, a prefix, or `0` for an empty schema. Default: latest |
| `--status` | report only; changes nothing |
| `--script` | write SQL instead of applying it; needs `--output` and opens no connection (ADR 0015) |
| `--from <m>` | with `--script`: start of the range |
| `--output <file>` | with `--script`: where the SQL is written |
| `--confirm-data-loss` | required for a downgrade that would lose data, applied or scripted (ADR 0015) |

The tool is **declarative**. You name a target, never a direction.

### `grant-roles`

- Creates the contained users and roles the application needs: the API's managed identity gets
  `db_datareader` + `db_datawriter`.
- The user's name is fixed: `barefoot-api`, the name of the API's user-assigned managed identity. There is
  no setting for it (ADR 0016).
- Idempotent: running it again changes nothing.
- Runs as part of `migrate` when the run ends on the latest migration, so a fresh database is usable by the
  API after one command. Not with `--status`, `--script`, or a move to an older migration.
- On a server that is not Azure SQL Database (a local SQL Server) the grant is skipped: the tool says so and
  exits `Ok` (0).

### `assign-owner <oid>`

- Before the application is connected to Entra there is one developer and no real object id. Local runs act
  as the seeded "Unassigned" user (`Guid.Empty`), who owns everything created in that time.
- Once sign-in works, `bf-maint assign-owner <oid>` moves every row that user owns to the user `<oid>`.
- `<oid>` must be a user who has signed in once (their `AppUser` row exists). Otherwise the tool exits
  `Refused` (10); an `<oid>` that is not a GUID exits `Usage` (4).
- The rows are the searches and recipients the user owns, the open issues raised for them, and the digest
  deliveries addressed to them. All move in one transaction, and each moved row gets a new `Version`.
- When `<oid>` already has a search of the same name, or a recipient of the same address, nothing moves:
  the tool names each collision and exits `Refused` (10).
- It is run once. Afterwards the "Unassigned" row stays seeded and owns nothing. A second run moves
  nothing and exits `Ok` (0). stdout carries one line per table with the number of rows moved.

### `verify-schema`

- Reads the live catalog and compares it with the model: tables, columns, indexes and foreign keys. It
  reports what is missing, what differs and what is extra, one line each on stderr, and changes nothing.
- No difference exits `Ok` (0); a difference exits `Failed` (1). A database behind or ahead of the build
  exits as `migrate --status` does and is not compared.
- It is built after CI has a SQL Server step (ADR 0016).

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

### What `migrate` writes and exits with

Without `--to` the target is the latest migration this build ships.

| The database is | The tool | Exit code |
|---|---|---|
| behind the target | applies the migrations up to it, oldest first | `Ok` (0) |
| at the target | changes nothing | `Ok` (0) |
| ahead of the target, and rolling back loses no data | rolls back the migrations after it, newest first | `Ok` (0) |
| ahead of the target, and rolling back would lose data | names each such step and changes nothing, unless `--confirm-data-loss` is given | `DataLossNotConfirmed` (6) |
| newer than this build | changes nothing and names the migrations it does not ship, whatever the target | `Refused` (10) |
| holding a gap: a migration is not applied and a later one is | changes nothing and names the missing migrations, whatever the target | `Refused` (10) |

On exit 0 stdout carries four lines: `direction:` (`up`, `down` or `none`), `applied:` with the ids it
applied, `reverted:` with the ids it rolled back, newest first, and `current:` with the id the database is
at afterwards. A list that is empty reads `none`. When a migration fails the tool exits `Failed` (1) with one
line on stderr.

A `--to` that matches no migration, or more than one, exits `Usage` (4) and changes nothing; the tool lists
the migrations it ships, or the ones the text matched. A whole id or name is preferred over a prefix. `--to`
and `--confirm-data-loss` are refused with `Usage` (4) beside `--status`.

### What `migrate --script` writes and exits with

`--script` opens no connection and needs no connection string. It writes the SQL that moves a database
from `--from` (default `0`, an empty schema) to `--to` (default the latest migration this build ships) to
the `--output` file, replacing a file that is already there, and exits `Ok` (0). The range comes from the
command line, never from a database.

stdout carries three lines: `script:` with the full path of the file, `direction:` (`up`, `down` or `none`),
and `migrations:` with the ids the script applies, oldest first, or rolls back, newest first. When the file
cannot be written the tool exits `Failed` (1).

When `--to` is before `--from` the script is a downgrade. It starts with a `-- Downgrade from <from> to
<to>.` comment, followed by each step that loses data, or by `-- No step loses data.` A downgrade that would
lose data needs `--confirm-data-loss`; without it the tool names the steps, writes nothing and exits
`DataLossNotConfirmed` (6).

These exit `Usage` (4) and write nothing: `--script` without `--output`, `--output` or `--from` without
`--script`, `--script` together with `--status`, and a `--from` or `--to` that matches no migration or more
than one.

`docs/EXIT-CODES.md` is generated from the enum.

**Enforced by:** the generated-file test that fails when the committed `docs/EXIT-CODES.md` differs from the
enum (the enum is in `Core`, phase 1; `bf-maint` is its first user, phase 2); the migrate-tool tests, run
in-process with injected writers, one per exit code (phase 2).

## Rules

### The database must exist

The tool never creates a database. A mistyped name must not create a stray database. The operator creates
the database (Azure, phase 6) and `bf-maint` fills it. When the database does not exist the tool exits
`DatabaseUnavailable` (8), as it does for one it cannot reach (ADR 0015).

**Enforced by:** the migrate-tool tests (`MigrateStatusTests`).

### A schema newer than the build is never touched

When the database holds migrations this build does not ship, the tool applies nothing, in either direction,
and exits `Refused` (10). Deploy the matching build, or restore a backup.

The API does not refuse to start on a newer schema. It reports the readiness state `NewerSchema` and answers
its data endpoints with `503 database.newer_schema`. It reads and writes nothing.

**Enforced by:** the migrate-tool tests (`MigrateApplyTests`, `MigrateStatusTests`); the readiness
state-machine tests (phase 2).

### A downgrade that would lose data needs confirmation

A downgrade is screened for data loss. These count:

- a dropped table;
- a dropped column;
- a narrowing column change;
- deleted seeded rows;
- raw SQL (the tool does not inspect it, so it treats it as data-losing).

Without `--confirm-data-loss` the tool exits `DataLossNotConfirmed` (6) and changes nothing. A downgrade
script carries a `-- Downgrade` header naming each such step.

**Enforced by:** the migrate-tool tests (`DataLossScreenTests`, `MigrateToTests`, `MigrateScriptRangeTests`).

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
- Logs go to **stderr and a rolling file** under the user's local application data:
  `Barefoot/logs/bf-maint-yyyyMMdd.log`, one file a day in UTC, the newest 90 kept. The file takes
  Information and above, stderr Warning and above (ADR 0016).
- When the log folder cannot be written the tool says so once on stderr and carries on; the exit code
  does not change.
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

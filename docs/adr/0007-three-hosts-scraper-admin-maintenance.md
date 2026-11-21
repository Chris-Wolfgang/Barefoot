# ADR 0007 — Three small hosts: scraper, admin, maintenance

- **Status:** Accepted, 2026-11-06 (amended 2026-11-20: logging and stdout rule for the three
  hosts; one `ExitCode` enum with a single numbering; `bf`'s two identities; the version check)
- **Scope:** `Wolfgang.Barefoot.Scraper`, `.Admin`, `.Maintenance`

## Context

Three unrelated kinds of unattended work exist: scraping (`refresh-*`, `run`, a diagnostic single
lookup), schema work (`migrate`), and "API, do X" steps (`send-digests`, status queries). Each has
different dependencies and a different place it can run: scraping needs Chromium and a residential
IP; schema work needs a direct DDL connection; API steps need only HTTPS and a token. One binary
with every verb would ship Chromium beside a 20-line HTTP call and attract every future verb by
gravity (the earlier tool's single CLI did exactly that).

## Decision

| Host | Binary | Depends on | Identity | Phase-1 verbs |
|---|---|---|---|---|
| `Scraper` | `bf-scraper` | `Obe`, `ApiClient`, `Core` | `Scraper` service principal | `refresh-airports`, `refresh-resorts`, `run [--resume]`, `search one` (diagnostic), `version` |
| `Admin` | `bf` | `ApiClient`, `Core` | `DigestSender` service principal in the scheduled job; run by hand, an interactive sign-in with the user's own roles (ADR 0005) | `send-digests [--run]`, `digests status`, `search list`, `settings …`, `version`; later `prune-vocab` |
| `Maintenance` | `bf-maint` | `Data`, `Core` | DDL identity | `migrate [--to] [--status] [--script]`, `verify-schema`, `grant-roles`, `assign-owner` |

- Each host has one reason to exist and one dependency set; the project graph and the architecture
  tests forbid the others (`Scraper` ⟂ `Data`; `Admin` ⟂ `Obe`, `Data`; `Maintenance` ⟂
  `ApiClient`, `Obe`).
- `bf` is the only host whose requirements are met **anywhere the API is reachable**: the home
  machine's job step now, a Container Apps cron job or a pipeline step later, with the same binary.
  That is why every "API, do X" step lives there and not in the scraper.
- One executable per host, many verbs, so each step is testable alone; the semiweekly job is a step
  list (`refresh-airports → refresh-resorts → run → send-digests`) gated on the exit code: stop on
  5, 7, 8 or 9 (nothing later can work), continue past 2 (partial) and 3 (breaker tripped — the
  digests for what did complete still go out).
- **Exit codes are one `ExitCode` enum in `Core`**, compiled into all three binaries, so a host can
  report "API unavailable" without having reached anything. It is not a database table. One
  numbering, no code meaning two things:

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

  `docs/EXIT-CODES.md` is generated from the enum and a test fails when the committed file differs,
  as with the error catalogue (ADR 0012 §1).
- **Every `bf-scraper` and `bf` verb except `version` checks `GET /version` first** and exits 5
  with a sentence saying which side to update (ADR 0010 §8).
- End-to-end testing is API contract tests (`WebApplicationFactory` + SQLite + `ApiClient`); the
  admin CLI exists for operations, not for testing.

## Not chosen

- *One "agent"/"worker" binary with every verb.* Honest name, wrong dependency set.
- *No admin CLI; UI only.* Loses scriptable steps (`send-digests`, `search list`) that a scheduled
  task and a cron job need.
- *Scheduling inside the API (a hosted service).* The scraper must run on a residential IP, and an
  API that scales to zero cannot host a timer.

## Consequences

- Binaries are `bf-*` (ADR 0009); the README and the scheduled task use those names.
- Exit codes live in one shared enum used by all three binaries; `bf-maint`'s codes are numbered in
  it, not beside it.
- All three log to stderr and a rolling file; stdout carries only the verb's output. An unhandled
  exception is one stderr line plus an exit code, with the stack trace in the file (DESIGN §14).
- Failures of the scheduled job are visible: the task's history reads the exit code, and a
  `report-failure`-style step opens one issue per failure signature.

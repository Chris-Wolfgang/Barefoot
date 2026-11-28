# ADR 0018 — A schema that does not match the build stops every tool

- **Status:** Accepted, 2026-11-27. Changes [ADR 0014](0014-exit-code-for-a-schema-behind-the-build.md)
  §1 (who uses code 11, and what the scheduled job does on it) and
  [ADR 0007](0007-three-hosts-scraper-admin-maintenance.md) (who uses codes 10 and 11, and the codes
  the job stops on). It carries out [ADR 0017](0017-answers-to-the-blocking-questions-of-2026-11-27.md) §6
  in the exit-code table
- **Deciders:** the owner, in review of ADR 0014
- **Scope:** `ExitCodes` in `Wolfgang.Barefoot.Core`; `bf-scraper`, `bf`, the scheduled job

## Context

ADR 0014 gave `bf-maint migrate --status` the exit code `MissingMigration` (11) and said "the
scheduled job continues past it, as it does past `Refused`". ADR 0007 lists codes 10 and 11 as used
by `bf-maint` only.

That left a gap. `bf-scraper` and `bf` never touch the database; they call the API. When the schema
does not match the API's build, the API answers its data endpoints with
`503 database.missing_migration` or `503 database.newer_schema` (ADR 0001 §4). No record said what
the two tools exit with then, and the table read as if the job went on.

The owner, reviewing ADR 0014: if any console or web application finds a migration missing, or the
database ahead of the build, it should stop at that point.

## Decision

1. **A tool that learns the schema does not match the build stops and says so.** It does no further
   work in that invocation.
2. **`bf-scraper` and `bf` exit `MissingMigration` (11)** when the API answers
   `503 database.missing_migration`, **and `Refused` (10)** when it answers
   `503 database.newer_schema`. They do not wait and retry: neither state ends on its own.
3. **The scheduled job stops on 10 and 11.** With ADR 0017 §6, the codes it stops on are 5, 7, 8,
   9, 10 and 11.
4. **Codes 10 and 11 are used by all three tools.** The numbers and names do not change, and
   `bf-maint` behaves as ADR 0014 says: `migrate --status` exits 11 on a database that is behind,
   `migrate` applies the pending migrations, and no verb touches a schema newer than the build.
5. **The web application already stops.** It renders the readiness state full-page, before the
   shell (ADR 0001 §4). The API itself still starts, so that it can report the state.

## Not chosen

- *`DatabaseUnavailable` (8) for both states.* The job would stop, but its report would read as an
  outage. The fix differs: run `bf-maint migrate`, or deploy the matching build.
- *Two new codes for `bf-scraper` and `bf`.* One situation would have two numbers, which ADR 0007
  forbids.
- *The API refusing to start.* ADR 0001 rejected it: a process that exits cannot say why.

## Consequences

- `docs/EXIT-CODES.md` is regenerated: codes 10 and 11 are used by all three tools and marked
  *stops*.
- ADR 0014's sentence "the scheduled job continues past it" no longer holds. The record is not
  rewritten.
- After a deployment that adds a migration, the scheduled job stops with 11 until `bf-maint migrate`
  has run. The job's failure report is the signal (Barefoot#81).
- `Refused` (10) from `bf-scraper` has two causes: a schema newer than the build and a refused
  catalog snapshot (ADR 0017 §6). The message says which.
- Nothing in `bf-scraper` or `bf` exits with these codes yet: the two tools arrive in phase 5. This
  record fixes the table they will be built against.

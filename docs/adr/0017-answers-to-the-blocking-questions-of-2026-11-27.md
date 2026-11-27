# ADR 0017 — Answers to the blocking questions of 2026-11-27

- **Status:** Accepted, 2026-11-27. Changes [ADR 0005](0005-identity-entra-invite-only.md) (the role
  is stored, for display) and [ADR 0011](0011-settings-registry.md) §3 (one more column), and
  reverses one acceptance criterion of Barefoot#110. It adds to
  [ADR 0001](0001-azure-sql-serverless-database.md), [ADR 0003](0003-api-owns-the-run-lifecycle.md),
  [ADR 0006](0006-email-stored-digests-separate-send.md),
  [ADR 0007](0007-three-hosts-scraper-admin-maintenance.md) and
  [ADR 0010](0010-api-client-hand-written-over-a-frozen-openapi-contract.md)
- **Deciders:** the owner, answering a list of questions put by the builder
- **Scope:** `Core`, `Contracts`, `Data`, `Business`, `Api`, `bf-maint`, `bf-scraper`, `bf`

## Context

A read of every open issue against the design found 15 points where a wrong guess would mean
reworking the schema, a public contract or an exit code. The owner answered 13 of them on
2026-11-27. This record holds those answers so that the documents they change can cite one place.
Two points stay open and are listed at the end.

## Decision

### Database

1. **`bf-maint` recognises a used-up free allowance now.** A small classifier in `Data` tells that
   error from other connection failures; `bf-maint` exits `QuotaExhausted` (9) on it. The API's
   readiness state machine reuses the classifier in phase 4.
2. **The text columns that must be unique have a binary collation**, `Latin1_General_100_BIN2`, so
   the database compares them as .NET's ordinal comparison does. Only those columns: the database's
   own collation is left alone.
3. **`Setting` gets `UpdatedByUserId`**, a foreign key to `AppUser`. `GET /settings` returns that
   person's display name as "updated by". ADR 0011 §3 lists the table without it.
4. **`AppUser` gets `Role`**, the role the person last signed in with, written at sign-in. It is for
   display on the Users page. What a caller may do is still decided from their token, as ADR 0005
   says; that record's "the role is not stored" no longer holds.
5. **A `catalog_refresh` table records each catalog snapshot**: which kind (airports or resorts),
   when, the counts of rows added, changed and deactivated, the warnings, and whether the snapshot
   was refused. "Reference data last refreshed" is read from it.

### Audit

6. **The audit tables of `Wolfgang.AuditTrail` go into `InitialCreate` now.** They are exempt, by
   table name, from the rules of `ModelConventions.Verify` that they cannot meet (a key that is not
   one `Guid`, a `DateTime` column). The library owns its tables.
7. **`Core` may reference `Wolfgang.AuditTrail.Abstractions`, and nothing else of that family**, so
   an entity can carry `[NotAudited]`. The dependency rule names this one exception.

### Contract

8. **A lookup has a third outcome: "not available".** `RoomsResult`, `FlightsResult` and
   `WorkStatus` each gain it. It means the site said the stay or the flights are not offered (a
   resort that is offline, flights beyond the booking horizon). It counts as done and not as failed.
9. **`/version` and `/readiness` are outside `/api/v{n}` and anonymous**, and touch no data.
   `/api/v0/system/schema` needs an Admin permission and answers while the database is not ready.
   The OpenAPI document is served at `/openapi/v0.json`, anonymous.
10. **`POST /digests/send` sends every pending delivery, whatever its run.** `bf send-digests`
    without `--run` calls it. A second call gives `DigestSender` the counts of deliveries by status
    and nothing else. The per-run route stays for `--run`.
11. **A failed delivery is not sent again on its own.** The next run brings a new digest; an old
    one is resent by hand from My digests or Deliveries. Barefoot#110's criterion "an undelivered
    digest is retried with the next run" is reversed by this.

### Before Entra exists

12. **Phases 4 and 5 run behind a Development-only switch.** A bootstrap key makes every caller the
    "Unassigned" user with every permission. The API refuses to start with the key set outside the
    Development environment. Contract tests use a test authentication handler.

### Scraper fixtures

13. **The builder never calls the live site.** Parser fixtures start from redacted pages of the
    earlier tool's archive. The owner later captures current pages from the home machine to confirm
    them.

## Not chosen

- *A binary collation for the whole database.* An Azure SQL database's collation is fixed when it
  is created, which `bf-maint` does not do, and every sort and search would become case-sensitive.
- *"Updated by" read from the audit trail.* A second query per key, and it ties the page to the
  audit library's shape.
- *Asking Microsoft Graph for a role.* ADR 0004 §8 chose `AppUser` to avoid Graph calls.
- *Changing AuditTrail first* (a `Guid` key, an opt-out set by `Data`). It makes phase 3 wait on
  another repository's release.
- *An empty list for "not available".* "No rooms" and "resort offline" would look the same, which
  Barefoot#82 forbids.
- *Registering the application in Entra now.* The build would wait on the owner through two phases.
- *Retrying failed deliveries with the next run.* An old digest arriving beside the new one is
  noise, and the open issue already tells the owner.

## Consequences

- `InitialCreate` is replaced several more times before the first release. The binary collation is
  written by hand after each replacement (`Migrations/OrdinalCollation.cs`); a test fails until it is.
- `Core` is no longer free of every package of the audit family. The architecture test allows
  `Abstractions` by name.
- The role shown on the Users page can be out of date: it changes only when the person signs in.
- The Development switch is a way around authentication. The start-up refusal outside Development
  is the only thing that keeps it out of production, so it needs its own test.
- The quota classifier is tested with made-up errors only. The real error number is still
  unverified (ADR 0001, "Known uncertainty") until a pause is seen on Azure.
- The archive pages may be out of date. A parser can pass its fixtures and still fail on the site
  until the owner's capture.
- Barefoot#110 needs its criterion reworded.
- **Still open:** how a refused catalog snapshot is reported (status, error code, exit code, whether
  the job goes on), and what state a run is in after an abort or a crashed worker (whether the next
  job resumes it, and for how long a run may be resumed). The owner asked to discuss both.

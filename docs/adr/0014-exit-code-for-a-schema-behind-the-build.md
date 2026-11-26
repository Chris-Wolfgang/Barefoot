# ADR 0014 — `migrate --status` exits 11 when the schema is behind the build

- **Status:** Accepted, 2026-11-25. Amends [ADR 0007](0007-three-hosts-scraper-admin-maintenance.md)
  (the `ExitCode` table gains code 11) and adds to
  [ADR 0001](0001-azure-sql-serverless-database.md) §4 (what `--status` exits with)
- **Scope:** `ExitCode` in `Wolfgang.Barefoot.Core`; `bf-maint migrate --status`

## Context

`bf-maint migrate --status` reports the schema version of the database against the one the build
expects (ADR 0001 §4). It finds one of three things: the database is at the expected version, it is
behind (a migration is pending), or it is newer than the build.

ADR 0007 gives the three hosts one `ExitCode` enum and the rule that no code means two things. It
assigns `Refused` (10) to a schema newer than the build. It assigns nothing to a schema that is
behind, and neither ADR says what `--status` exits with (gap M-4 in
`docs/notes/2026-11-20-convention-gaps.md`).

A script that runs `--status` needs the answer from the exit code. The verb's output is for a
person; a release pipeline or an operator's script should not have to parse it to learn whether
`migrate` has work to do.

## Decision

1. **`ExitCode` gains `MissingMigration` = 11**: `migrate --status` found the database behind this
   build. Only `bf-maint` uses it. The scheduled job continues past it, as it does past `Refused`.
   The name is the name of the readiness state the API reports for the same situation (ADR 0001 §4).
2. **`migrate --status` exits by what it finds:**

   | The database is | Exit code |
   |---|---|
   | at the version this build expects | `Ok` (0) |
   | behind this build | `MissingMigration` (11) |
   | newer than this build | `Refused` (10) |

3. **Only `--status` exits 11.** `bf-maint migrate` on a database that is behind applies the pending
   migrations and exits 0.
4. **A schema newer than the build keeps code 10 for every verb.** `--status` and `migrate` report the
   same situation with the same number.

## Not chosen

- *Exit 0 whenever `--status` could read the database.* Then 0 means both "at the expected version"
  and "behind", which ADR 0007 forbids, and a script has to parse the output.
- *Reuse `Failed` (1) for "behind".* The verb did not fail; it reported. A pipeline could not tell a
  pending migration from a crash.
- *A new code for "newer" as well.* ADR 0001 and ADR 0007 already give that situation code 10, and
  the operator's action is the same whichever verb found it: deploy the matching build. A second
  code would give one situation two numbers. If `Refused` later gains an unrelated meaning, it can
  be split then.

## Consequences

- `docs/EXIT-CODES.md` is regenerated and gains the row; `docs/MIGRATE.md` restates the table.
- A script can write `bf-maint migrate --status`, read 11, and then run `bf-maint migrate`.
- A caller that treats every non-zero exit code as an error now sees one for a database that is
  merely behind. The scheduled job does not run `bf-maint`, so it is not affected.
- Still open in gap M-4: the exit code when the database does not exist, for an ambiguous `--to`
  prefix, and for `--output` without `--script`.

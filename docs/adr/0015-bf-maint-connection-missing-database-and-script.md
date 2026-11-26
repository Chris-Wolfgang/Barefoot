# ADR 0015 — `bf-maint`: where the connection comes from, a missing database, and `--script`

- **Status:** Accepted, 2026-11-25. Adds to [ADR 0001](0001-azure-sql-serverless-database.md) §4 and
  [ADR 0007](0007-three-hosts-scraper-admin-maintenance.md); it reverses nothing in either
- **Scope:** `Wolfgang.Barefoot.Maintenance` (`bf-maint`)

## Context

ADR 0001 §4 and ADR 0007 describe `bf-maint migrate` and leave four points open. They are gaps M-2,
M-4, M-5 and M-6 in `docs/notes/2026-11-20-convention-gaps.md`:

- how the tool is given its connection (M-2);
- what it exits with when the database does not exist (M-4);
- whether `--script` needs `--output`, and whether it connects (M-5);
- whether a scripted downgrade needs `--confirm-data-loss` (M-6).

The tool cannot be built without an answer to each.

## Decision

1. **The connection string is the configuration key `ConnectionStrings:Barefoot`.** The tool reads it
   from the environment variable `ConnectionStrings__Barefoot` or, on a developer's machine, from
   user secrets. There is no command-line flag for it: a connection string on a command line ends up
   in shell history and in process listings. When the key is missing or blank the tool exits `Usage`
   (4) and opens nothing.
2. **A database that does not exist exits `DatabaseUnavailable` (8).** The tool cannot tell a
   missing database from one it cannot reach, and the operator's next step is the same: check the
   name, the firewall rule and the identity. The tool still never creates a database.
3. **`--script` requires `--output` and opens no connection.** The SQL is rendered from the
   migrations the build ships, so a script can be written and reviewed on a machine that cannot
   reach the database.
4. **A scripted downgrade that would lose data needs `--confirm-data-loss`**, as an applied one
   does. A script is written to be run; the screen applies to what it would do.

## Not chosen

- *A `--connection` flag.* Convenient for a one-off, but it puts the secret in shell history.
- *`Refused` (10) or `Usage` (4) for a missing database.* `Refused` says the tool chose not to act
  on a database it could read; `Usage` says the arguments were wrong. Neither is true when the
  database cannot be opened at all.
- *`--script` writing to stdout when `--output` is absent.* stdout carries the verb's own output
  (ADR 0007), and a script printed there is easy to truncate or to mix with a later line.
- *No confirmation for a scripted downgrade.* The script would then carry a data-losing step that
  nothing asked the operator about.

## Consequences

- `bf-maint` references the configuration packages for environment variables and user secrets.
- A wrong database name and a stale firewall rule give the same exit code and the same sentence;
  the operator has to check both.
- The release pipeline sets `ConnectionStrings__Barefoot` for the job that runs `bf-maint`.
- Still open: the exit code for an ambiguous `--to` prefix and for `--output` without `--script`
  (the rest of M-4), and gaps M-1, M-3 and M-7 to M-11.

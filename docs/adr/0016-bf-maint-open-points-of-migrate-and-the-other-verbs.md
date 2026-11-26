# ADR 0016 — `bf-maint`: the open points of `migrate`, and `grant-roles`, `assign-owner`, `verify-schema` and logging

- **Status:** Accepted, 2026-11-26. Adds to [ADR 0001](0001-azure-sql-serverless-database.md) §4,
  [ADR 0005](0005-identity-entra-invite-only.md), [ADR 0007](0007-three-hosts-scraper-admin-maintenance.md)
  and [ADR 0015](0015-bf-maint-connection-missing-database-and-script.md); it reverses nothing in them
- **Scope:** `Wolfgang.Barefoot.Maintenance` (`bf-maint`); the logging of all three command-line hosts

## Context

ADR 0015 ended with points still open: the rest of gap M-4, and gaps M-1, M-3, M-7 and M-11 in
`docs/notes/2026-11-20-convention-gaps.md`. Building `migrate` raised more: what a "narrowing column
change" is, what the verb prints, what it does with a history that has a gap, and what a downgrade
script's header looks like. This record answers them. Gaps M-8, M-9 and M-10 stay open.

## Decision

### `migrate`

1. **`Usage` (4) for a command line that is wrong.** That covers options that do not go together
   (`--output` without `--script`, `--from` without `--script`, `--to` beside `--status`), and a
   `--to` or `--from` that matches no migration or more than one. Nothing is read from a database to
   reach either answer.
2. **The data-loss screen counts five kinds of step:** a dropped table, a dropped column, a
   narrowing column change, deleted seeded rows, and raw SQL. A column change narrows when a maximum
   length, a precision or a scale shrinks or goes from unlimited to limited; when the CLR type
   changes, in either direction; or when the column type text changes and no length or precision
   says which way it went. A change of nullability or of a default alone does not count.
3. **`migrate` prints four lines on stdout**, the same for every move: `direction:` (`up`, `down`
   or `none`), `applied:`, `reverted:` and `current:`.
4. **A history with a gap is refused with `Refused` (10).** A gap is a shipped migration that is
   not applied while a later shipped one is. The tool never leaves one, so it means the history was
   changed by hand. The tool names the missing migrations and changes nothing, with or without
   `--to`. `--status` still reports on such a database.
5. **A downgrade script starts with a header:** `-- Downgrade from <from> to <to>.`, then
   `-- Steps that lose data:` and one line per step, or `-- No step loses data.` The header names
   the steps that lose data, not every migration rolled back; stdout lists those.
6. **`--from` equal to `--to` writes an empty file and exits `Ok` (0)**, with `direction: none`.

### `grant-roles`

7. **The API's database user has the fixed name `barefoot-api`.** It is the name of a user-assigned
   managed identity, so it does not depend on what the Container App is called. There is no
   configuration key for it.
8. **On a server that is not Azure SQL Database the grant is skipped.** A local SQL Server cannot
   create a user from Entra. `grant-roles` and `migrate` both say that they skipped it and exit `Ok` (0).
9. **`migrate` grants only when the run ends on the latest migration.** Not with `--status`, not
   with `--script`, not after a move to an older migration or to `0`.

### `assign-owner <oid>`

10. **It moves four columns** from the "Unassigned" user to `<oid>`: `SearchMatrix.OwnerId`,
    `Recipient.OwnerId`, `OpenIssue.UserId` and `DigestDelivery.RecipientUserId`.
11. **A collision moves nothing.** When `<oid>` already has a search of the same name or a recipient
    of the same address, the tool names each collision and exits `Refused` (10). It does not rename
    or merge.
12. **One transaction**, and `Version` is incremented on every moved row. The change is audited as
    the system user: the tool made it and no one was signed in.
13. **Exit codes:** `<oid>` is not a GUID, `Usage` (4); `<oid>` has no `AppUser` row or is the
    "Unassigned" user itself, `Refused` (10); nothing to move, `Ok` (0). stdout carries one line per
    table with the number of rows moved.

### `verify-schema`

14. **It is a read-only drift check.** It reads the live catalog and compares it with the model:
    tables, columns (name, type, nullability), indexes and foreign keys. It reports what is missing,
    what differs and what is extra.
15. **Exit codes:** no drift, `Ok` (0); drift, `Failed` (1) with one line per difference on stderr.
    When the history is behind or ahead of the build it exits as `migrate --status` does (11 or 10)
    and compares nothing.
16. **It is built after CI has a SQL Server step.** The check is about SQL Server's catalog; a
    version tested only on SQLite would prove little.

### Logging of the command-line hosts

17. **The log folder is `Barefoot/logs` under the user's local application data**
    (`%LOCALAPPDATA%\Barefoot\logs`; `~/.local/share/Barefoot/logs`). One folder for all three hosts.
18. **One file per host per day**, `<host>-yyyyMMdd.log` (`bf-maint-20261126.log`), times in UTC.
    The newest 90 files per host are kept.
19. **The file takes Information and above; stderr takes Warning and above.** A run with nothing
    wrong prints nothing on stderr.
20. **Serilog writes the file**, behind `Microsoft.Extensions.Logging`
    (`Serilog.Extensions.Logging`, `Serilog.Sinks.File`). Code logs through `ILogger` only.
21. **The shared setup lives in its own project, `Wolfgang.Barefoot.Hosting`**, so `Core` and the
    API and Web hosts do not reference a file logger.
22. **A log folder that cannot be written does not fail the run.** The tool says so once on stderr,
    carries on with stderr only, and the exit code does not change.

## Not chosen

- *`Refused` (10) for a `--to` that matches nothing.* `Refused` says the tool read a database and
  chose not to act on it.
- *Leaving deleted seeded rows out of the screen.* They are rows, and a rollback removes them.
- *Moving a database with a gap, reporting `down` or `mixed`.* It acts on a history someone edited,
  on a guess.
- *`Usage` (4) for an empty script range.* A pipeline that computes the range can arrive at
  "nothing to do", and plain `migrate` on a database that is up to date also exits 0.
- *A configuration key for the identity's name.* One more thing to set and to get wrong, for a
  system with one deployment.
- *Renaming or merging on a collision in `assign-owner`.* The operator knows which of the two rows
  to keep; the tool does not.
- *Dropping `verify-schema`.* `migrate --status` trusts the history table; nothing else looks at
  the tables themselves.
- *A file writer of our own.* Rolling and retention would be our code to test.
- *The logging setup in `Core`.* Every project that references `Core` would get the Serilog packages.

## Consequences

- A second environment in the same tenant cannot have its own API identity name without a new
  record.
- A change of CLR type that loses nothing (`int` to `long`) still asks for `--confirm-data-loss`.
- A migration that seeds rows into an existing table needs `--confirm-data-loss` to be rolled back.
- A database with a gap has to be repaired by hand before the tool will move it.
- The grant cannot be tested on SQLite, which has no users or roles. Tests pin the SQL the tool
  sends and the skip; the grant first runs on Azure in phase 6.
- In the release pipeline the log file is lost with the runner; stderr in the job log is the record.
- Whether EF Core's schema reader can be used for `verify-schema` without the design-time package
  is not known yet.
- Still open: gaps M-8 (order of deploy and migrate), M-9 (the pipeline's access) and M-10 (a
  `version` verb).

# Database conventions

This document is the contract for the Barefoot schema and for the `Wolfgang.Barefoot.Data` project. It
restates ADR 0004 (keys, concurrency, conventions, one migration), ADR 0001 (provider, portability, the
migration rules), ADR 0002 §3 (data-access rules), ADR 0005 (audit identity) and DESIGN §8 / §11. It adds no
decision.

The `Data` project is being built in phase 2; a rule that is not enforced yet says which phase brings it. Each rule names what enforces it and the build phase (DESIGN §13,
phases 0–7) that enforcement arrives with. Points the ADRs leave open are listed in [the gaps note](notes/2026-11-20-convention-gaps.md)
and are marked here as *(open: gap D-n)*.

## Provider and portability

- Production is Azure SQL Database (free offer, serverless). The test provider is SQLite. There is no
  second production provider.
- The Data layer contains **no raw SQL** and **no provider-specific column types** outside the migration.
  The one exception is the condition of the run lock's filtered index, `status = 0`
  (`RunConfiguration.InProgressFilter`): EF takes an index filter only as SQL text. It names one column
  that needs no quoting and one number, so SQL Server and SQLite both read it.
- Only the API and `bf-maint` hold a connection string. Every other host is a client of the API.
- The API's database identity is `db_datareader` + `db_datawriter`. It never changes the schema. Only
  `bf-maint` does ([MIGRATE.md](MIGRATE.md)).

**Enforced by:** the architecture tests "UI and worker projects reference no data packages" and the
project-reference rules of DESIGN §2 (arrive with phase 0); the test suite running the same model on SQLite
(phase 2); `ProviderPlacementTests`, which fail when code of the `Data` project outside `Migrations/` names
the provider; CI applying the migration to SQL Server (phase 2); review checklist line: "no raw SQL and no
provider-specific type outside `Migrations/`" (pr-gate skill, phase 0) *(open: gap D-1)*.

## Names

Every name is snake_case. Constraints and indexes carry a prefix:

| Object | Prefix |
|---|---|
| primary key | `pk_` |
| alternate key | `ak_` |
| foreign key | `fk_` |
| index | `ix_` |
| unique index | `ux_` |

The rest of the pattern (decided 2026-11-23, closing gap D-2; it is the `wms` shape):

| Object | Name | Example |
|---|---|---|
| table | the entity's name, singular | `search_matrix`, `app_user` |
| column | the property's name | `owner_id`, `adult_count`, `total_amount` |
| primary key | `pk_<table>` | `pk_recipient` |
| alternate key | `ak_<table>_<columns>` | |
| foreign key | `fk_<table>_<columns>` | `fk_recipient_owner_id` |
| index | `ix_<table>_<columns>`; unique `ux_<table>_<columns>` | `ux_recipient_owner_id_address` |

- A key column keeps its property's name, so `app_user.oid` and `setting.key` are not `id`. `key` is a
  reserved word in SQL Server; EF quotes it, and hand-written SQL must write `[key]`.
- Every table is in the default schema.
- No configuration names a table, column or constraint by hand. `SchemaNames.Apply` names them all, last in
  `OnModelCreating`. The property names themselves follow "Names" in
  [CODING-CONVENTIONS.md](CODING-CONVENTIONS.md).

**Enforced by:** `SchemaNames.Apply`, with `SchemaNamesTests` and `BarefootDbContextTests` (phase 2);
`ModelConventions.Verify`, asserted empty by `ModelConventionsTests` on SQLite. It also fails a table outside
the default schema and a name longer than 128 characters, the most SQL Server accepts.

## Types

Decided 2026-11-23, closing the enum part of gap D-1, gap D-11 and the money part of gap D-12.

| Value | Stored as | Set by |
|---|---|---|
| an instant (`DateTimeOffset`) | a UTC date and time with no offset, to the millisecond: `datetime2(3)` on SQL Server | `UtcInstantConverter`, for every `DateTimeOffset` in the model |
| money (`decimal`) | `decimal(10,2)`: up to 99,999,999.99 | `BarefootModel.ConfigureConventions`, for every `decimal` in the model |
| an enum | its number (`int`) | EF's default; renaming a member needs no migration |
| text | a length from `ColumnLengths`, one length per kind of text; a digest's two bodies alone have no length | each configuration |

An instant is stored without its offset because SQLite cannot sort or compare a `DateTimeOffset` column in
SQL, and paging and lease expiry do both. The offset is always zero.

**Enforced by:** `BarefootModel.ConfigureConventions`, with `BarefootDbContextTests` and `BarefootModelTests` (phase 2). String
lengths and the hash column are still open *(gap D-12)*.

## Referential integrity

- Every foreign key has `Restrict` delete behaviour. There are no cascades.
- A delete is an explicit business operation. It removes the dependent rows itself, in the same operation.
  Example (ADR 0006): deleting a search or a recipient removes its `SearchRecipient` link rows.

**Enforced by:** `ModelConventions.Verify`, asserted empty by `ModelConventionsTests` on SQLite.

## Keys

- Every key is a `Guid` (`uniqueidentifier`). One convention covers the model.
- The user key is given: `AppUser.Oid` is the Entra object id. `OwnerId` is that object id, parsed once at
  the authentication boundary.
- Ids are minted by the API. A client never supplies the id of a row it creates.
- Hot tables use **sequential** Guids for clustered-index locality: `SearchRequest`, `RoomResult` and the
  flight rows *(open: gap D-3)*.
- `Setting` is keyed by its `Key` (the setting's name).
- `Guid.Empty` is the seeded "Unassigned" user (below). No real object id equals it, so no query treats it
  specially.

**Enforced by:** `ModelConventions.Verify`, which fails a primary key that is not one `Guid` column on any
table but `setting`; review checklist line: "a new hot table uses
sequential Guids" (pr-gate skill, phase 0).

## Concurrency: the portable `Version` column

- `Version` is a `long` concurrency token, incremented by the context.
- The context owns the number. `SaveChanges` stores a new row with `Version` 1 and a changed row with the
  version it was read with plus one; whatever a caller put in the property is replaced. A deleted row's
  version is only checked.
- A save made from an older version writes nothing and throws `DbUpdateConcurrencyException`. A caller that
  holds a version from elsewhere (an `If-Match` tag) states it by setting the property's original value.
- SQL Server `rowversion` is **not** used. It is not available on the SQLite test provider, and the portable
  form keeps one code path.
- There is no global row version and no sequence behind it.
- `Version` is also the row's ETag. A stale edit is `412`; an edit without a precondition is `428`
  ([API-CONVENTIONS.md](API-CONVENTIONS.md)).
- ADR 0004 §10 puts `Version` on: `SearchMatrix`, `SearchRequest`, `FlightSet`, `Setting`, `Resort`,
  `Airport`, `AppUser`, `Recipient` *(open: gap D-4 — DESIGN §8 also shows it on more reference tables)*.
- The rule behind the list: user-edited rows and claimable rows carry `Version`.
- A uniqueness rule is not a concurrency token. The run lock is the filtered unique index on
  `Runs(Status) WHERE Status = InProgress`, not a `Version` check.
- If a collection ETag or a cache stamp is ever needed, it uses `max(UpdatedUtc)` + count, or a per-table
  generation row. The settings cache already works this way ([SETTINGS.md](SETTINGS.md)).

**Enforced by:** `BarefootDbContextVersionTests`; `ModelConventions.Verify`, which fails a `Version` that is
not a `long` concurrency token; the run-lifecycle
checkpoint tests over SQLite, which
race a retry against the original (phase 3); the API contract tests for `412` / `428` (phase 4).

## Uniqueness rules in the model

| Table | Unique on | Serves |
|---|---|---|
| `SearchMatrix` | `(OwnerId, NormalizedName)` | search names unique per owner; `NormalizedName` is trimmed, upper-invariant; a race is `409` |
| `Run` | `Status`, filtered to `InProgress` | the run lock: at most one run in progress |
| `SearchRequest` | `(Run, Resort, CheckIn, CheckOut, AdultCount, ChildCount, InfantCount)` | dedupe per run; `Cabin` is not in the key |
| `SearchItem` | `(MatrixRun, SearchRequest)` | one link per search wanting the request |
| `MatrixRun` | `(Run, SearchMatrix)` | a search takes part in a run once |
| `RoomResultFeature` | `(RoomResult, VocabularyValue)` | a result names a feature once |
| `SearchItemFlightSet` | `(SearchItem, FlightSet)` | a search item links to a flight lookup once |
| `FlightDirection` | `(FlightOption, IsReturn)` | an option has one direction each way |
| `FlightLeg` | `(FlightDirection, Sequence)` | a place in a direction is taken once |
| `Recipient` | `(OwnerId, Address)` | the owner's address book; `Address` is trimmed, lower-cased; a duplicate is `409` |
| `SearchRecipient` | `(SearchMatrix, Recipient)` | which recipients a search mails |
| `SearchResort` | `(SearchMatrix, Resort)` | a search names a resort once |
| `SearchDepartureAirport` | `(SearchMatrix, Airport)` | a search names a departure airport once |
| `SearchRoomFilter` | `(SearchMatrix, VocabularyValue)` | a search filters by a value once |
| `Airport` | `Code` | a code is one airport; a catalog snapshot finds each airport by it |
| `Resort` | `Code` | a code is one resort; a catalog snapshot finds each resort by it |
| `RoomCategory` | `(Resort, Code)` | a room's code is unique within its resort |
| `VocabularyValue` | `(Kind, Value)` | a value is unique within its vocabulary |
| `FlightSet` | per run: `(origin airport, gateway airport, depart date, return date, cabin, travellers)` | flight sets deduped across the run (DESIGN §6) |

The `FlightSet` row is from DESIGN §6, which gives the key but does not mark it `UNIQUE` in the §8 sketch
*(open: gap D-9)*.

**Enforced by:** the Business tests for name uniqueness, dedupe at explode and the run lock (arrive with
phase 3); the API contract tests for the `409`s (phase 4).

## Indexes

- **Every index cites the query it serves.**
- Every sortable field of a list endpoint is backed by an index that ends in the row id
  ([API-CONVENTIONS.md](API-CONVENTIONS.md), Paging).
- An index nobody can name a query for is not added.

**Enforced by:** review checklist line: "every index cites the query it serves" (pr-gate skill, phase 0)
*(open: gap D-5)*.

## Time

- Every instant is a `DateTimeOffset` in UTC.
- `TimeProvider` is injected. `DateTimeOffset.Now` and `DateTimeOffset.UtcNow` are banned.
- `ResortSpan` uses the sentinels `1900-01-01` and `9999-12-31` for an open start and an open end.
- An airport's IANA zone is `Airport.TimeZoneId`, nullable.

**Enforced by:** the architecture test "no `DateTime` on a public surface" (phase 0); the ban on
`DateTimeOffset.Now` / `UtcNow` (phase 0) *(open: gap D-6)*; `ModelConventions.Verify`, which fails a
`DateTime` column and an instant not stored in UTC to the millisecond.

## Migrations

- The model ships as **one** migration, `InitialCreate`. There is no earlier schema. It lives in
  `src/Wolfgang.Barefoot.Data/Migrations/` and is written for SQL Server. Until the first release a change
  to the model replaces it: `dotnet ef migrations remove`, then `dotnet ef migrations add InitialCreate`,
  both run in the `Data` project. `dotnet ef` does not write `// <auto-generated />` at the top of the
  migration file itself; add it as the first line, or the analyzers fail the build on generated code.
- A text column of a unique index or key has the collation `Latin1_General_100_BIN2`, so the database
  compares it as .NET's ordinal comparison does. The collation is written in the migration and not in the
  model: its name belongs to SQL Server, and SQLite already compares by bytes. `dotnet ef` does not write
  it. After replacing `InitialCreate`, add `collation: OrdinalCollation.Name` to each column listed in
  `Migrations/OrdinalCollation.cs`. No other column names a collation.
- Every later migration has a working `Down`.
- CI applies up → down → up against SQL Server.
- Raw SQL and provider-specific column types are allowed in a migration and nowhere else. Raw SQL in a
  migration is treated as data-losing on a downgrade ([MIGRATE.md](MIGRATE.md)).
- Only `bf-maint migrate` applies migrations. The API never migrates.
- Backup restore is the primary rollback. `Down` is for the pipeline and for development.
- Renaming a setting is a migration that copies the row ([SETTINGS.md](SETTINGS.md)).

**Enforced by:** `scripts/check-migration.sh`, which applies the migration up → down → up on a SQL Server
container and checks that the script from `--script` builds the same schema (run by the Migration Check
workflow);
the migrate-tool tests, run in-process with injected writers (phase 2); the model-convention test on SQLite,
which keeps `InitialCreate` in step with the conventions (phase 2); `InitialCreateTests`, which fail when the
model holds a change the migration does not, and which read the SQL the migration renders for SQL Server
without opening a connection. They also fail when a text column of a unique index or key lacks the binary
collation, or when the list in `OrdinalCollation` misses such a column.

## Audit

- `Wolfgang.AuditTrail` audits **configuration and reference entities**. DESIGN §8 marks as audited:
  `SearchMatrix`, `Recipient`, `Setting`, `Resort`, `ResortSpan`, `Airport`, `RoomCategory` and the
  vocabularies.
- **Results and execution tables are `[NotAudited]`** *(open: gap D-7 — the full list is not written
  down)*.
- `user_id` names the calling application (web, scraper, `bf`). `on_behalf_of` is the signed-in person's
  `oid` when there is one.
- `HttpContextAuditUserProvider` records the user's `oid` and display name, or the service principal's name
  for the scraper's and the digest sender's writes. `SystemAuditUserProvider` names the job for unattended
  work.
- Catalog changes made from a snapshot are audited as the scraper's identity (DESIGN §5).
- No secret is ever stored in an audited table: the audit trail shows values.

**Enforced by:** review checklist line: "a new entity is audited or carries `[NotAudited]` with the reason"
(pr-gate skill, phase 0); the Business tests that assert an audited change names its caller (phase 3)
*(open: gap D-7)*.

## Data-access rules (ADR 0002 §3)

- EF's `DbContext` is the unit of work. There is no generic repository.
- One query class per read, with an `AsNoTracking` projection.
- No `IQueryable` crosses a Core interface.
- No `Set<T>()` on the unit-of-work surface.
- `ToEntity` lives in `Data`. `ToDto` lives in `Business`. A read may project straight to a `Contracts`
  record.
- Registration is explicit. There is no assembly scanning.
- A lookup's results are written in one `SaveChanges`. No explicit transaction is wrapped by hand: every
  Data service inherits the retrying execution strategy from the context (ADR 0001 §1).
- EF Core command logging is at Warning (DESIGN §14).

**Enforced by:** the architecture test "conversion methods live where the conventions say" (arrives with
phase 0); review checklist lines for the query-class, `IQueryable` and `Set<T>()` rules (pr-gate skill,
phase 0) *(open: gap D-8)*.

## Seed row

`AppUser` is seeded with one row: `Guid.Empty`, "Unassigned". It owns whatever is created before the
application is connected to Entra. `bf-maint assign-owner <oid>` moves those rows to a real user once
([MIGRATE.md](MIGRATE.md)). Afterwards the row stays and owns nothing.

The row is part of the model (`AppUserConfiguration`), so the migration creates it with the table. Its
e-mail address is empty, it was last seen on 1900-01-01 (never), its `Version` is 1, and every other column
has its default.

## Adding an entity

1. Class in `Core` with a `Guid` key, `DateTimeOffset` instants and `Guid` foreign keys.
2. Decide whether it is user-edited or claimable. If so, add `Version` (`long`, concurrency token).
3. Decide whether it is configuration / reference data (audited) or a result / execution table
   (`[NotAudited]`).
4. `IEntityTypeConfiguration` in `Data`: `Restrict` on every foreign key; a unique index for every
   uniqueness rule in the design; every index cites the query it serves.
5. Register the configuration explicitly in the `DbContext`. No assembly scanning.
6. Hot table? Use sequential Guids.
7. No raw SQL and no provider-specific column type in the configuration.
8. Add a migration with a working `Down`. (Until the first release there is only `InitialCreate`.)
9. Reads: one query class per read, `AsNoTracking`, returning a projection. `ToEntity` in `Data`, `ToDto` in
   `Business`.
10. Run the model-convention test. It fails the build if a convention is broken.
11. If the row is exposed for editing: `ETag` / `If-Match` on its endpoints
    ([API-CONVENTIONS.md](API-CONVENTIONS.md)).

**Enforced by:** `ModelConventions.Verify`, asserted empty by `ModelConventionsTests` on SQLite; the pr-gate
migration review (phase 0).

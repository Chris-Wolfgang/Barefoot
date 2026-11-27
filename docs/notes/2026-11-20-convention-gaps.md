# Gaps found while writing the convention documents (2026-11-20)

Gaps, ambiguities and contradictions found while restating DESIGN.md and ADR 0001–0012 as
`API-CONVENTIONS.md`, `DATABASE-CONVENTIONS.md`, `MIGRATE.md` and `SETTINGS.md`. Nothing here is a decision.
Each item says what the sources state, what they leave open, and what the document did about it.

Sources read in full: DESIGN.md (2026-11-06, with the 2026-11-20 amendments), ADR 0001–0012,
`docs/notes/2026-11-06-wms-reuse-review.md`, the Settings lines of `docs/mockups/v2/README.md`, and the six
wms documents for shape only.

## Top items

1. **`bf-maint verify-schema` is named and never described** (M-1).
2. **ADR 0012 contradicts itself on `3xx`**: "never returns a `3xx`" and "`If-None-Match` answers `304`" (A-2).
3. **The `Version` column list differs between ADR 0004 §10 and DESIGN §8** (D-4).
4. **Most error codes have no name yet**, including every `412`, `428`, `400` and `404` (A-1).
5. **The setting types are not stated**; only names, defaults and six validators are (S-1).
6. **`GET /settings` returns "updated-by", but `Setting` has no such column** (S-3).

## Cross-cutting

- **X-1. Enforcement is mostly unnamed in the ADRs.** The ADRs describe tests ("an architecture test fails
  an endpoint that does neither", "a test fails when the served document differs") but name almost none.
  Named things the documents could cite: `ModelConventions.Verify`, the `openapi-diff` CI job, the
  `[Operation("…")]` coverage test, `docs/api/frozen.txt`, `docs/api/errors.md`, `docs/EXIT-CODES.md`,
  `DatabaseReadiness`, `RunLifecycle`, `ScrapeLoop`. The documents therefore describe each test in the ADR's
  words and invent no class name. `ModelConventionsTests` is the wms name for that test; no
  Barefoot source uses it, so the documents do not.
- **X-2. Review checklist lines are my wording.** Where no automated check is described, the document pairs the
  rule with a "review checklist line … (pr-gate skill, phase 0)". DESIGN §13 says the pr-gate skill arrives
  in phase 0 but does not list its lines. Every such line is a proposal for that skill, not a restatement.
  Rules that currently have only this: handlers pick an `ErrorCode`; a new write names its repeat-safety;
  sortable field has an index; no raw SQL outside the migration; sequential Guids on hot tables; index cites
  its query; audited or `[NotAudited]`; query-class / `IQueryable` / `Set<T>()` rules; settings read only
  through `ISettings`; no secret is a setting; key ships with its reader; no `Console.Write`.
- **X-3. Phase attribution is inferred in places.** DESIGN §13 puts the architecture test project in phase 0,
  typed keys in phase 1, Data + `bf-maint` in phase 2, Business + the registry in phase 3, Api + `ApiClient`
  in phase 4. I assigned: readiness state-machine tests to phase 2 (`DatabaseReadiness` is a singleton in
  `Data`), though the readiness *endpoint* is phase 4; `ExitCode` and its generated-file test to phase 1/2;
  codecs to "phase 1 or 3"; the unknown-key start-up warning to phase 4. None of these is stated.
- **X-4. An architecture scanner that arrives in phase 0 has nothing to scan until later** (endpoints in
  phase 4, pages in phase 7). The documents say "scanner in phase 0; endpoints from phase 4". Confirm that is
  the intended reading of "the architecture test project with its positive-control fixtures" in phase 0.
- **X-5. `docs/API-VERSIONING.md`.** DESIGN §15 names it as where versioning lands; the phase 0 list in
  DESIGN §13 does not list it; versioning is folded into `API-CONVENTIONS.md`. One of the three should
  change, or the document's Versioning section should be split out.
- **X-6. `DELIVERY-PROCESS.md` and `CODING-CONVENTIONS.md`** are phase 0 documents the four documents lean on
  (banned APIs, typed keys, pr-gate). They are written separately, in the same stack.
- **X-7. "Phase 1" has two meanings.** ADR 0007's table header says "Phase-1 verbs" (first product phase);
  ADR 0006 says self-service recipients are "phase 2" (product phase); DESIGN §13 numbers build phases 0–7.
  `bf settings …` is a "Phase-1 verb" in ADR 0007 but is missing from DESIGN §13 phase 5's verb list
  (`send-digests`, `digests status`, `search list`, `version`). The documents give no build phase for
  `bf settings`.

## API-CONVENTIONS.md

- **A-1. Unnamed error codes.** Named in the sources: `internal.error`, `auth.not_signed_in`,
  `auth.forbidden`, `search.name_taken`, `search.active_limit_reached`, `run.attempt_superseded`, and the
  five `database.*` codes. No name for: duplicate recipient address (409), over the recipient cap (422), a
  run already in progress (409), `POST /runs/resume` with nothing to resume (404), `result` repeated with a
  different body (422), stale `If-Match` (412), missing `If-Match` (428), invalid cursor / unknown sort
  (400), not found (404), validation failures (422), an unknown setting key, an invalid setting value. The
  document lists only the named ones.
- **A-2. `3xx` contradiction.** ADR 0012 §1: "The API never returns a `3xx`". §3: "`If-None-Match` answers
  `304`". The status table in §2 has no `304` row. The document keeps the table verbatim and reads "never a
  `3xx`" as "never redirects". The ADR needs a one-line amendment.
- **A-3. `Retry-After` "is set when known".** Not said for which states or with what value. Plausible:
  `quota_exhausted` (until `ResetsAtUtc`), `waking`. Not written into the document.
- **A-4. Problem-detail members.** The ADR fixes `code`, `severity`, `traceId` and the `ErrorCode` fields
  (status, template, docs anchor, severity). It does not say what `type`, `title` and `detail` hold (wms:
  `title` = template with arguments, `type` = docs page at the anchor). For `500` it says "the detail is the
  trace id". Severity values are not enumerated. Where the docs anchor points (`docs/api/errors.md`?) is not
  said.
- **A-5. The `Unknown` readiness state** has no code. A request in `Unknown` presumably starts the probe and
  sees `database.waking`. Not stated.
- **A-6. Order of readiness and authentication.** The API reads the caller's `AppUser` row per request for
  revocation (ADR 0005), which needs the database. So a data endpoint during `Waking` must answer `503`
  before it can decide `401`-for-revoked. Whether an *unauthenticated* call in that state gets `401` or
  `503` is not stated. It matters because the web host renders readiness "before the shell".
- **A-7. Which routes are outside the version prefix, and which are anonymous.** ADR 0010 §3: "every route
  lives under the version prefix". Is that true of `GET /version`, `GET /readiness`, `GET /system/schema`
  and the served OpenAPI document? A version probe under `/api/v0/` works only while the client knows which
  `v` to ask. Only `GET /version` is stated to be anonymous. `GET /readiness` is "always answerable"; its
  permission is not given. `GET /system/schema` has no permission stated, and it needs the database while
  describing a database that may not be ready. The served OpenAPI path is not given.
- **A-8. The revision after `v1`.** The revision is "raised by one with every breaking change to `v0`".
  After `v1` freezes there are no breaking changes, yet §8 says "only *too old* (an API whose revision is
  below the client's) fails". What raises the revision after the freeze (additive changes a client depends
  on?) is not stated. The field names of the `GET /version` body are not given either.
- **A-9. What counts as breaking** is not listed (wms has a list). The `openapi-diff` job decides. The
  environment flag that regenerates the committed document is not named. Whether `v0` stays served as an
  alias after `v1` is not decided.
- **A-10. `PUT` or `PATCH`; `200` or `204`.** Both verbs and both answers are allowed; no rule says which an
  endpoint uses, or whether a successful write returns the new `ETag`. "`ApiClient` carries the tag from the
  DTO it last read" implies every versioned DTO has a `Version` member; only the settings entry is said to.
- **A-11. Weak tags.** "Comparison is strong." A weak tag in `If-Match` is presumably `412` (wms says so);
  not stated. I left it out.
- **A-12. Writes on rows with no `Version`.** `If-Match` is required "on a versioned row". Unversioned rows
  that are changed through the API: `OpenIssue` (acknowledge), `SearchRecipient`, `DigestDelivery` (resend),
  `ResortSpan` / `RoomCategory` / vocabularies if D-4 resolves that way. No rule for them.
- **A-13. Repeat-safety of the remaining `POST`s.** The table covers six calls and four are declared not
  repeat-safe. Not classified: resend a digest (each call is "a further attempt", so not repeat-safe by
  design?), acknowledge an Open issue, accept the disclaimer, Sign out everywhere, activate / deactivate a
  search, post a catalog snapshot. ADR 0010 §7's "any other `POST`" covers how `ApiClient` retries them, so
  the client is safe; the table is just incomplete. Routes for several of these are not in DESIGN §5.
- **A-14. Status of a repeated `complete`.** "Returns the same outcome" — status code not given.
- **A-15. Paging details.** Not stated: both `after` and `before` given (wms: `400`); the default sort per
  endpoint; `size` below 1 or above 200 (clamped or `400`?); whether a client `size` on the flight-options
  endpoint is ignored or refused; `firstItemIndex` zero- or one-based (the UI shows "26–50"); a `size` that
  changes between pages while the index travels in the cursor.
- **A-16. Dates that are not instants.** "Every instant is `DateTimeOffset` in UTC." Check-in and check-out
  are calendar dates; recurring windows are month/day pairs. Their wire type (`DateOnly`, ISO date string)
  is not stated. The architecture rule bans `DateTime` on public surfaces, which is compatible with
  `DateOnly`, but nothing says so.
- **A-17. Changing a search's `recipientIds` without `Permission.RecipientsWrite`** (ADR 0006 §2). The
  endpoint's declared permission is presumably "edit search", so this is not the endpoint-level `403` of
  ADR 0012 §2. `403`, `422` or silently ignored — not stated.
- **A-18. Request compression** and compression of problem details are not mentioned. Left out.
- **A-19. `Cache-Control` on non-data responses** (`/version`, `/readiness`, errors) is not stated.

## DATABASE-CONVENTIONS.md

- **D-1. "No raw SQL, no provider-specific types outside the migration" has no stated check.** Running the
  model on both SQLite and SQL Server catches most of it, not all. The document adds a review line.
  Related: the run lock is a *filtered* unique index. In EF the filter is a SQL string in the model
  configuration (`HasFilter`), which is raw SQL outside the migration in the letter of the rule, and its
  text depends on how the `Status` enum is stored. Enum storage (string or number) is not stated for the
  database; ADR 0012 only says enums are strings in JSON.
- **D-2. The name pattern is only half decided.** Decided: snake_case and the five prefixes. Not decided:
  table names singular or plural (entities are singular; DESIGN §5 and ADR 0003 write `Runs(Status)`); the
  primary-key column name (`AppUser.Oid` and `Setting.Key` show it is not always `id`); foreign-key column
  naming (`OwnerId`, `RecipientUserId`, `UserId` all point at `AppUser`); the `<prefix>_<table>_<columns>`
  shape; the database schema (`dbo` or a named one); where the EF migrations history table lives. The document
  states the prefixes' meanings (from the wms shape) and nothing more.
- **D-3. Sequential Guids: the generator is not chosen.** Options differ in portability: an EF client-side
  sequential generator works on SQLite; a `NEWSEQUENTIALID()` default is provider-specific. "Ids are minted
  by the API" suggests the former. "Flight rows" is not enumerated (`FlightSet`, `FlightOption`,
  `FlightDirection`, `FlightLeg`?). Whether the primary key is the clustered index is assumed, not stated.
- **D-4. Which rows carry `Version`.** ADR 0004 §10: `SearchMatrix`, `SearchRequest`, `FlightSet`,
  `Setting`, `Resort`, `Airport`, `AppUser`, `Recipient`. DESIGN §8's sketch ends "Resort, ResortSpan,
  Airport, RoomCategory, vocabularies (+ Version)", which adds `ResortSpan`, `RoomCategory` and the
  vocabularies. The document lists the ADR's eight and flags the difference. Also not stated: the starting
  value (a setting's first insert is `Version` 1; others presumably the same) and what happens to `Version`
  on bulk updates that bypass the change tracker.
- **D-5. "Every index cites the query it serves": where and checked how.** Not stated whether the citation
  is a comment in the entity configuration or in the migration, nor what checks it. In wms it is a pr-gate
  line; the document assumes the same. Also not stated: whether every foreign-key column must be indexed (wms's
  verifier requires it).
- **D-6. The ban on `DateTimeOffset.Now` / `UtcNow`** is stated without a mechanism (a banned-symbols list is
  the fleet's usual one). `CODING-CONVENTIONS.md` should name it.
- **D-7. Audit coverage is not enumerated.** Marked "audited" in DESIGN §8: `SearchMatrix`, `Recipient`,
  `Setting`, and the reference line. Not marked either way: `AppUser` (role-relevant fields such as
  `SessionsValidAfterUtc`), `SearchRecipient`, `OpenIssue`, `DigestDelivery`. "Results and execution tables
  are `[NotAudited]`" is not expanded into a list. Also open: the audit tables' names and whether the
  convention verifier exempts library-owned tables; whether the library's own timestamp type fits
  "`DateTimeOffset` everywhere"; and DESIGN §15's "AuditTrail gaps to re-check", which is not resolved in
  any ADR. The audit column is written `on_behalf_of` in DESIGN and ADR 0005 and `on_behalf_of_user_id` in
  the reuse note.
- **D-8. DAL rules without a stated check.** ADR 0002 §3 gives four rules; only conversion placement has an
  architecture test (DESIGN §14). "One query class per read", "no `IQueryable` across a Core interface" and
  "no `Set<T>()`" have none. The second is cheap to check by reflection over `Core`; that would be a new
  test, so it is only noted.
- **D-9. `FlightSet` uniqueness.** DESIGN §6 says a flight set is "keyed per run by (origin airport, gateway
  airport, depart date, return date, cabin, travellers)". DESIGN §8 does not mark a `UNIQUE` on it as it
  does for `SearchRequest`. The document lists it and flags it. "Travellers" is one value here and three
  columns (adults, children, infants) on `SearchRequest`.
- **D-10. What `ModelConventions.Verify` checks** is not listed beyond ADR 0004 §12. The document attributes
  naming, `Restrict`, key type, concurrency token and UTC instants to it. Confirm that list.
- **D-11. `DateTimeOffset` on SQLite (my knowledge, not from the sources).** The EF Core SQLite provider
  cannot order or compare `DateTimeOffset` columns in SQL without a value converter. Keyset paging sorts on
  instants and lease expiry compares them, and the contract tests run on SQLite. A converter choice is a
  decision the ADRs do not make; it may also touch "no provider-specific column types outside the
  migration". Worth settling before phase 2. I have not verified this against the EF Core version in use.
- **D-12. Column types and lengths.** No rule for money (`decimal` precision for prices), string lengths, or
  the hash column (`ResultSha256`: binary or hex text).
- **D-13. `InitialCreate` and `Down`.** ADR 0001: "every migration has a working `Down`". DESIGN §8: "every
  *later* migration has a working `Down`". DESIGN §13 phase 2 runs up → down → up on the single migration,
  so `InitialCreate` needs one too. The document follows DESIGN's sentence and the CI rule.
- **D-14. Where the seed row is written** (`HasData` in the migration, or by `bf-maint`) is not stated.

## MIGRATE.md

- **M-1. `verify-schema` has no description anywhere.** It appears in DESIGN §2, §13 and ADR 0002 / 0007 by
  name only. Candidates: the `{current, expected, upToDate}` probe (but `migrate --status` already reports
  that); or the model-convention check run against the live SQL Server catalog. Its output and exit codes
  are unknown. The document marks the row open.
- **M-2. How `bf-maint` is given its connection.** No flag or configuration key is named. The `Database`
  options section exists (ADR 0001 §1); the connection-string key does not have a name. The document says it
  uses the same options as the API; that is my reading of "after warm-up", supported by the reuse note.
- **M-3. `grant-roles` inputs and scope.** How the tool learns the API's managed-identity name is not
  stated. "Runs as part of `migrate`": also on `--status`, `--script`, a downgrade, `--to 0`? Not stated.
- **M-4. Exit code when the database does not exist.** wms folds it into "unreachable". Barefoot's enum has
  `DatabaseUnavailable` (8), `Refused` (10) and `Usage` (4); none is assigned. Similarly unassigned: an
  ambiguous `--to` prefix (wms: usage error), `--status` on a schema that is behind (0?) or ahead (10?),
  `assign-owner` with an `<oid>` that has no `AppUser` row, `--output` without `--script`.
- **M-5. Is `--output` required with `--script`?** ADR 0001 writes `--script [--from] [--to] --output`
  (not bracketed); ADR 0007 writes `migrate [--to] [--status] [--script]`. The document shows it as given in
  ADR 0001. Whether `--script` needs no connection at all (wms) is implied by "offline" only.
- **M-6. Does a *scripted* downgrade need `--confirm-data-loss`?** wms: yes. ADR 0001 says the script
  "carries a `-- Downgrade` header naming each step" and does not say. The screen's list (dropped tables /
  columns, narrowing changes, raw SQL) is shorter than the wms list; the document copies ADR 0001's.
- **M-7. `assign-owner` details.** "Every row it owns" is not enumerated (`SearchMatrix.OwnerId`,
  `Recipient.OwnerId`, `OpenIssue.UserId`, `DigestDelivery.RecipientUserId`?). Not stated: whether the move
  is audited and under which identity (the tool bypasses the API; `SystemAuditUserProvider`?), whether
  `Version` is incremented, whether a name collision with the target's own searches can occur, and what a
  second run does (nothing to move → `Ok`?).
- **M-8. Order of deploy and migrate.** The states imply migrate first (the old API then reports
  `NewerSchema` until the matching build is deployed) but no ADR states an order. The document's five-step
  operator checklist is derived from the readiness states; treat it as a proposal.
- **M-9. The release pipeline's access.** ADR 0001 §5 allow-lists "the machine that runs `bf-maint`". When
  the pipeline runs it, its address and identity need the same; not covered.
- **M-10. No `version` verb for `bf-maint`.** `bf-scraper` and `bf` have one; ADR 0007 lists none for
  `bf-maint`. It also makes no `GET /version` check (correct: it never calls the API).
- **M-11. Log file location** is "under the user's local application data"; the path is not given.

## SETTINGS.md

- **S-1. Types.** ADR 0011 §11 gives names and defaults, §10 gives six validators. No CLR type is stated.
  All but one are whole numbers with units in the name, so the document writes "integer †"; `int` or `long` is
  open. `TimeSpan` has a codec but no key uses it. Three keys have no validator stated:
  `run.politeness_jitter_ms`, `digest.sender_display_name`, `vocab.stale_after_runs`. A negative jitter or
  an empty sender name is therefore accepted as written.
- **S-2. Codec text formats** are not given (wms lists them). Which phase the codecs arrive with (Core in 1
  or Business in 3) is not stated.
- **S-3. "Updated-by" has no column.** *(Answered 2026-11-27: `Setting` gets `UpdatedByUserId`, a foreign
  key to `AppUser`.)* `GET /settings` returns "updated-by/at" (ADR 0011 §2). The table is
  `Key`, `Value`, `UpdatedUtc`, `Version` (§3, DESIGN §8). Either a column is missing or the value is read
  from the audit trail. The mockup README also shows "who last changed it".
- **S-4. Bootstrap key names.** Only `Hosting:BehindProxy` and the `Database` option names are given. Not
  named: the application's section, the connection-string key, the Entra keys, the CORS allow-list key, the
  two session-lifetime keys and their defaults, the e-mail credential's key. The "Host" column of the
  document's bootstrap table is my reading. `WarmUpGraceSeconds` sits in the API's `Database` section but
  describes how long *a UI* waits; the web host has no `Database` section, so how it learns the value is
  open. Whether a wms-style `CONFIGURATION.md` is wanted is not in DESIGN §13's phase 0 list.
- **S-5. The unknown-key warning.** "An unknown key under the application's section produces a startup
  warning." Not stated: which hosts do this (API only, or all five), the section's name, and whether keys
  from environment variables are covered. No test is described; the document proposes one.
- **S-6. The default of `digest.sender_display_name` is "the product name".** ADR 0009 makes the product
  name "a display string" used in several places; where that string is defined (a constant, bootstrap
  configuration) is not stated, so the setting's default has no source yet.
- **S-7. Settings error codes and bodies.** Unknown key (404?) and a value the validator rejects (422 by
  ADR 0012 §2) have no code names. The `PUT` body shape is not given.
- **S-8. `Version` 0 recurs after a reset.** `DELETE` removes the row, so the setting reports `Version` 0 /
  `ETag: "0"` again. A client still holding `"0"` from before the first write can then write without having
  seen the intervening value. Probably acceptable at this size; noting it because it is the one place where
  the ETag is not monotonic. `DELETE` on a setting that has no row (`If-Match: "0"`) has no stated answer.
- **S-9. The "Read by" column** of the registry table is assembled from DESIGN §3–§7 and ADR 0005 / 0006 /
  0011. `run.max_active_searches_per_user` is "enforced in the BLL"; the exact operation (activate, create,
  reactivate) is not stated.
- **S-10. Mockup wording.** The Settings page details (Save on an edited row, Reset on a changed row, "What
  is running" with scraper last seen) come from `docs/mockups/v2/README.md`, which DESIGN §12 points to.

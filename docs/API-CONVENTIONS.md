# API conventions

This document is the contract for every endpoint of `Wolfgang.Barefoot.Api`. It restates ADR 0012 (errors,
status codes, concurrency, repeat-safe writes, paging), ADR 0010 (versioning, the OpenAPI document, the
contract revision, `GET /version`), ADR 0005 (401 / 403) and ADR 0001 (readiness `503`s). It adds no decision.

Nothing described here is built yet. Each rule names what enforces it and the build phase (DESIGN §13,
phases 0–7) that enforcement arrives with. Points the ADRs leave open are listed in [the gaps note](notes/2026-11-20-convention-gaps.md)
and are marked here as *(open: gap A-n)*.

Routes are written without the version prefix for brevity, as in DESIGN and the ADRs. Every route lives
under `/api/v{n}` (see [Versioning](#versioning)).

## Errors: problem details with a typed code

- Every error response is `application/problem+json`.
- Every problem detail carries three extension members: `code`, `severity` and `traceId`.
- `code` is a typed `ErrorCode` declared in `Core`. An `ErrorCode` has a lower-case dotted name, an HTTP
  status, a message template, a docs anchor and a severity.
- Message templates use positional placeholders. They are validated when the API starts.
- An unhandled exception becomes `500 internal.error`. Its detail is the trace id, never the exception.
- Every log entry for an API call carries the same `traceId` the problem detail reports (DESIGN §14).
- The API never redirects. A caller who is not signed in gets `401`, not a redirect to a sign-in page.
- `docs/api/errors.md` is generated from the `ErrorCode` registry (code, status, message template, severity,
  anchor). It is never edited by hand.

`ErrorCode`, `Permission` and `SettingKey<T>` share one typed-key base in `Core`.

Codes the ADRs name:

| Code | Status | When |
|---|---|---|
| `internal.error` | 500 | unhandled exception |
| `auth.not_signed_in` | 401 | no valid token |
| `auth.forbidden` | 403 | the caller lacks the permission the endpoint declares |
| `search.name_taken` | 409 | a search with that name already exists for the owner |
| `search.active_limit_reached` | 422 | the owner is at `run.max_active_searches_per_user` |
| `run.attempt_superseded` | 409 | a `result` for an expired or superseded attempt |
| `database.waking` | 503 | readiness, below |
| `database.quota_exhausted` | 503 | readiness, below |
| `database.missing_migration` | 503 | readiness, below |
| `database.newer_schema` | 503 | readiness, below |
| `database.unavailable` | 503 | readiness, below |

The other conditions in the status table have no code name yet *(open: gap A-1)*.

**Enforced by:**
- the `ErrorCode` type itself: names and templates are validated at construction and at startup
  (arrives with phase 1; startup validation with phase 4);
- the generated-catalogue test: it fails when the committed `docs/api/errors.md` differs from the registry
  (arrives with phase 4);
- the API contract tests over `WebApplicationFactory` + SQLite through `ApiClient` (arrive with phase 4);
- review checklist line: "a handler picks an `ErrorCode`; it never writes a status code or a message of its
  own" (pr-gate skill, phase 0).

## Status codes

| Status | When |
|---|---|
| 200 | read, an update that returns the row, or an action that reports its outcome (`digests/send`, ADR 0006) |
| 201 + `Location` | create |
| 204 | delete, or an update with no body |
| 400 | malformed request, invalid cursor, unknown sort |
| 401 | no valid token (`auth.not_signed_in`) |
| 403 | the caller lacks the **permission the endpoint declares** (`auth.forbidden`) — an Editor calling an Admin endpoint. Decided before any row is looked up, so it reveals nothing about data |
| 404 | not found **or not visible to the caller** (never 403 for another user's row — nothing reveals another user) |
| 409 | uniqueness or state conflict: duplicate search name or recipient address, a run already in progress, a `result` for a superseded attempt |
| 412 | `If-Match` did not match the row's `Version` (stale edit), or the row changed between check and save |
| 422 | the request is well-formed but the rules reject it: validation, a limit such as `run.max_active_searches_per_user` (`search.active_limit_reached`), a `result` repeated for the same attempt with a different body |
| 428 | a change or delete without `If-Match`, or with `*` |
| 503 | readiness (below) |

- There is no `429`. The callers are a handful of invited users and two service principals. The limits that
  matter are business rules, answered with `422`.
- `304` is answered to `If-None-Match` (see [Concurrency](#concurrency-version-is-the-etag)). "The API never
  returns a `3xx`" means it never redirects *(open: gap A-2)*.
- `409` and `412` are different on purpose. `412` means someone else changed the row: reload and retry.
  `409` means the name is taken or the state forbids it: choose another.

**Enforced by:** the API contract tests (phase 4); the committed OpenAPI document, which lists every status
an operation returns and is reviewed with the PR (phase 4).

## Authentication and authorization (401 / 403 / 404)

- The API validates Entra JWTs itself. It is the one gate for the web host, `bf-scraper` and `bf`.
- `401 auth.not_signed_in`: no valid token. A token issued before the caller's *sessions valid after*
  instant (`AppUser.SessionsValidAfterUtc`) is also `401`.
- `403 auth.forbidden`: the caller lacks the permission the endpoint declares. The decision is made before
  any row is looked up.
- `404`: the row does not exist or the caller may not see it. Another user's row is never `403`.
- Every endpoint declares `RequirePermission(Permission.X)`, or `AllowAnonymous()` with a comment that says
  why. Permissions are typed keys (`Permission`), mapped to roles in one table.
- `GET /version` is anonymous and never touches the database.
- Authentication endpoints are never response-compressed (BREACH).

**Enforced by:** the architecture test that fails any endpoint declaring neither a permission nor
`AllowAnonymous()` (scanner and positive-control fixture arrive with phase 0; it has endpoints to scan from
phase 4); the API contract tests (phase 4).

## Readiness: `503` with a typed code

The API always starts. `GET /readiness` is always answerable. Every *data* endpoint is gated on the
readiness state of ADR 0001 and answers `503` with one of these codes until the state is `Ready`:

| Readiness state | Code | What the caller is told |
|---|---|---|
| `Waking` | `database.waking` | the database is resuming; "Warming up. Please wait…" after `WarmUpGraceSeconds` |
| `QuotaExhausted` | `database.quota_exhausted` | the monthly free allowance is used up; names the reset instant (00:00 UTC on the first day of the next month) |
| `MissingMigration` | `database.missing_migration` | "Missing migration: `<names>`. Run `bf-maint migrate`." |
| `NewerSchema` | `database.newer_schema` | "The database is newer than this build. Deploy the matching API version." The API reads and writes nothing |
| `Unavailable` | `database.unavailable` | the retry budget is spent; `LastError` is set |

- `Retry-After` is set when it is known *(open: gap A-3)*.
- `Unavailable` is re-probed only when a request arrives, at most once every 30 seconds, never from a timer.
- `GET /system/schema` answers `{current, expected, upToDate}` from the same code path `bf-maint` uses.
- `ApiClient` retries a readiness `503` inside the retry budget of ADR 0001 §1. A headless host that runs out
  of budget exits with a code from the shared `ExitCode` enum ([MIGRATE.md](MIGRATE.md) has the table).

**Enforced by:** the readiness state-machine unit tests, driven by a fake connector and a fake clock
(arrive with phase 2, where `DatabaseReadiness` lives in `Data`); the API contract tests for the `503`
mapping (phase 4).

## Concurrency: `Version` is the ETag

- A read of a single row returns `ETag: "<Version>"`. The tag is strong.
- `If-None-Match` with the current tag answers `304`.
- Every `PUT`, `PATCH` and `DELETE` on a versioned row requires `If-Match` with that tag.

| Request | Answer |
|---|---|
| no `If-Match` | `428` |
| `If-Match: *` | `428` |
| `If-Match` does not equal the row's `Version` | `412` |
| the row changed between the check and the save | `412` |
| `If-Match` equals the row's `Version` | the change is applied |

- Comparison is strong (RFC 9110).
- A row that does not exist yet but has a readable default (a setting, ADR 0011) reports `Version` 0 and
  `ETag: "0"`. Its first write is `If-Match: "0"`.
- Data responses carry `Cache-Control: private, no-cache`. Never `no-store`. Never `max-age`.
- `ApiClient` carries the tag automatically from the DTO it last read. It is the only place that knows about
  ETags. Screens see typed results and `PreconditionFailedException` (`412`) or `ConflictException` (`409`).
- The web UI answers a `412` on save by saying the row changed elsewhere and offering Reload (DESIGN §12).

Which rows carry `Version` is in [DATABASE-CONVENTIONS.md](DATABASE-CONVENTIONS.md).

**Enforced by:** the API contract tests for `428` / `412` / `304` (phase 4); the
model-convention test for the concurrency token itself (phase 2).

## Every write is repeat-safe by its own state

There is no `Idempotency-Key` header. There is no table of recorded calls. Each write a client may have to
repeat is safe because of state the model already holds:

| Call | What makes a repeat safe |
|---|---|
| `result` (request or flight set) | the `attempt` token, below |
| `complete` | the search's stage for the run: a search already *Aggregated* returns the same outcome and stores no second digest |
| `digests/send` | it only ever sends *Pending* deliveries (ADR 0006); a repeat finds none |
| any `PUT` / `PATCH` / `DELETE` | `If-Match` (above): a repeat of an applied change is `412` |
| create a search | the name is unique per owner (ADR 0004): a repeat is `409 search.name_taken` |
| create a recipient | the address is unique per owner (ADR 0006): a repeat is `409` |

**The attempt token.**

- `claim` mints an opaque `Guid` attempt and stores it on the request (or flight-set) row.
- `result` carries the attempt in its body.
- The row is the record. Its `Attempt`, `Status` and `ResultSha256` columns answer a repeat:

| `result` arrives with | Answer |
|---|---|
| the current attempt, first time | stored; the body's hash is kept in `ResultSha256` |
| the same attempt, the same body | the first outcome again; nothing is stored twice |
| the same attempt, a different body | `422` |
| an expired or superseded attempt | `409 run.attempt_superseded`; nothing changes |

- The check and the write are one `SaveChanges` under the retrying execution strategy (ADR 0001 §1). No
  transaction is wrapped by hand. The row's `Version` makes a retry that races the original lose (ADR 0003).
- Ids are minted by the API. A client never supplies the id of a row it is creating.

**Calls that are deliberately not repeat-safe:** `claim`, `POST /runs`, `abort`, `finish`.

**What `ApiClient` retries (ADR 0010 §7).**

| Call | Retried automatically on |
|---|---|
| `GET`; any call carrying `If-Match` or an `attempt` token; `complete`; `digests/send` | a connection failure, a timeout, or a readiness `503` |
| any other `POST` (`claim`, `POST /runs`, `abort`, `finish`, a create) | only when the request provably never reached the API: the connection could not be opened, or the answer was a readiness `503` |

After a timeout or a dropped response the second group is **not** repeated; the caller gets the failure. A
blind retry of `claim` would lease a second request and strand the first until its lease expired. The
scraper recovers through the run: `POST /runs/resume`, then `claim` again. A create that did land answers
the user's own retry with `409`.

**Enforced by:** the run-lifecycle checkpoint tests, server-side over SQLite (arrive with phase 3); the API
contract tests, which repeat each call in the table and assert the second answer (phase 4); review checklist
line: "a new write endpoint names the state that makes a repeat safe, or is listed as not repeat-safe"
(pr-gate skill, phase 0).

## Paging: keyset, opaque cursors

List endpoints take:

| Parameter | Meaning |
|---|---|
| `after` / `before` | opaque cursors |
| `sort` | one field name; a leading `-` means descending |
| `size` | default 25, cap 200 |

They return:

| Member | Meaning |
|---|---|
| `items` | the page |
| `nextCursor`, `previousCursor` | opaque cursors for the neighbouring pages |
| `firstItemIndex` | the index of the page's first item |
| `totalCount` | the number of rows the list had when it was first requested |

- A cursor is bound to its sort. Under another sort it is refused with `400`.
- An invalid cursor or an unknown sort is `400`.
- **The cursor carries the position and the count.** The total is counted once, on the request without a
  cursor. It travels inside the cursor with the index of the page's first item. Later pages return both
  without a second `COUNT`. A list that changed underneath shows a slightly stale total until it is reloaded.
- Every sortable field is backed by an index that ends in the row id.
- The flight-options endpoint takes its page size from the setting `results.flight_options_page_size`
  (default 10), price ascending ([SETTINGS.md](SETTINGS.md)).
- The UI shows "Showing 26–50 of 62" with Previous / Next. There are no page numbers.
- `ApiClient` is the only place that knows about cursors.

**Enforced by:** the API contract tests (phase 4); review checklist line: "every sortable field has an index
ending in the id, and the index cites this query" (pr-gate skill, phase 0;
[DATABASE-CONVENTIONS.md](DATABASE-CONVENTIONS.md)).

## Compression, time, naming

- Data responses are compressed with Brotli or gzip. Authentication endpoints are **never** compressed
  (BREACH).
- Every instant is a `DateTimeOffset` in UTC, written as ISO 8601. The client renders it in the user's zone.
- JSON is camelCase.
- DTOs are records in `Contracts`, shared by `Api` and `ApiClient`.
- Enums are strings.
- Clients ignore fields they do not know.

**Enforced by:** the architecture test "no `DateTime` on a public surface" (arrives with phase 0); the
`System.Text.Json` source-generated context shared by `Api` and `ApiClient`, so both sides serialize the
same way (phase 4); the committed OpenAPI document (phase 4).

## Versioning

- The API is versioned by path: `/api/v{n}/…`. Every route lives under the version prefix.
- `/api/v0/…` is **unstable**. It may break. A breaking change carries a `breaking` changelog fragment.
- `v1` **freezes at 1.0**. After that it changes only additively.
- `docs/api/frozen.txt` lists the frozen versions.

### The OpenAPI document is the contract

- The API serves its OpenAPI document (`Microsoft.AspNetCore.OpenApi`).
- A copy is committed at `docs/api/openapi-v{n}.json`.
- The committed document must equal the served one. To change it, regenerate it with an environment flag and
  commit it in the same PR as the API change.
- Every `operationId` in the committed document has exactly one `ApiClient` method. The binding is an
  `[Operation("…")]` attribute. There is no naming convention beside it. Every `ApiClient` method names an
  operation that exists.
- A PR that touches the API carries the document diff. Reviewers read the document.

### Contract revision and `GET /version`

- The contract carries a **revision**: an integer constant in `Contracts`, written into the committed
  OpenAPI document. It is raised by one with every breaking change to `v0`.
- `GET /version` is anonymous and never touches the database. It returns the API's build version, the
  contract version (`v0`) and the revision.
- `ApiClient` compares that revision with the one it was compiled against, before its first real call.

| Phase of the contract | API revision lower than the client's | API revision higher than the client's |
|---|---|---|
| during `v0` (the two must be equal) | too old: "The API is older than this tool. Deploy the API." | too new: "This tool is older than the API. Update it." |
| after `v1` freezes | too old: fails | works (the contract only grew) |

- `bf-scraper` and `bf` print the sentence and exit `VersionMismatch` (5). Every verb of theirs except
  `version` makes this check first. The scheduled job stops on 5.
- The web host shows a full-page state.

### Adding or changing an endpoint

1. Map it in the minimal API under the version prefix, with `RequirePermission(...)` or `AllowAnonymous()`
   and its reason.
2. Add the member to the Core interface and the DTO records to `Contracts`.
3. Add the `ApiClient` method with its `[Operation("…")]` attribute.
4. Regenerate `docs/api/openapi-v{n}.json` and commit it in the same PR.
5. If the change breaks `v0`: raise the contract revision and add a `breaking` changelog fragment.
6. If it adds an `ErrorCode`: regenerate `docs/api/errors.md`.
7. Add or extend the contract test that drives it through `ApiClient`.

**Enforced by:**
- the test that fails when the served OpenAPI document differs from the committed one (arrives with phase 4);
- the `openapi-diff` CI job: it fails a PR that changes a version listed in `docs/api/frozen.txt`, fails a
  PR whose `v0` diff is breaking and leaves the revision unchanged, and only reports the diff otherwise
  (phase 4);
- the coverage test over `operationId` ↔ `[Operation]` (phase 4);
- the contract tests over `WebApplicationFactory` + SQLite driving the real `ApiClient` — the only
  integration suite and the end-to-end gate (phase 4);
- the client-side revision check in `ApiClient` (phase 4), used by `bf-scraper` and `bf` (phase 5) and the
  web host (phase 7).

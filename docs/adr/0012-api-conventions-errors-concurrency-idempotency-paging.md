# ADR 0012 — API conventions: typed error codes in problem details, ETag concurrency, idempotency by state, keyset paging

- **Status:** Accepted, 2026-11-20 (proposed 2026-11-06; amended on acceptance: the attempt token,
  paging position and count, the 401/403/404/422 rules, a generated error catalogue; later the same
  day: the `Idempotency-Key` header and its `IdempotencyRecord` table are dropped — every write is
  repeat-safe by its own state — `digests/send` answers `200`, and a newer schema is a readiness
  code)
- **Scope:** `Api` (every endpoint), `ApiClient`, `Contracts`, `Core` (`ErrorCode`)

## Context

Every client — web, scraper, admin tool, tests — talks to one API (ADR 0002). Errors, optimistic
concurrency, retries and paging need one answer each, decided once, or every endpoint will invent
its own. The scraper in particular retries over a flaky home connection and must never double-write
a lookup's results (ADR 0003); the web UI edits rows that another tab or the Admin may also edit;
Results and flight options are long lists read on phones.

`Chris-Wolfgang/wms` has a settled `docs/API-CONVENTIONS.md`; this ADR adopts its rules, adapted to
a per-row `Version` instead of a global row version. The full conventions document is written in
phase 0 (DESIGN §13); this ADR records the decisions it will contain.

## Decision

### 1. Errors are problem details with a typed code

- Every error response is `application/problem+json` carrying `code` (a typed `ErrorCode` from
  `Core`: lower-case dotted name, HTTP status, message template, docs anchor, severity),
  `severity` and `traceId`. Templates use positional placeholders and are validated at startup.
- Unhandled exceptions become `500 internal.error` problem details; the detail is the trace id,
  never the exception.
- Readiness (ADR 0001) answers `503` with `database.waking`, `database.quota_exhausted`,
  `database.missing_migration`, `database.newer_schema` or `database.unavailable`; `Retry-After`
  is set when known.
- The API never returns a `3xx`; authentication failures are `401 auth.not_signed_in` and
  `403 auth.forbidden` (ADR 0005).
- `docs/api/errors.md` is **generated** from the `ErrorCode` registry (code, status, message
  template, severity, anchor) and a test fails when the committed file differs — the same
  arrangement as the OpenAPI document (ADR 0010). It is never edited by hand.

### 2. Status codes

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
| 503 | readiness (above) |

There is no `429`: the callers are a handful of invited users and two service principals, and the
limits that matter are business rules answered with `422`.

### 3. Concurrency: `Version` is the ETag

- Reads of a single row return `ETag: "<Version>"`; `If-None-Match` answers `304`.
- Every `PUT`/`PATCH`/`DELETE` on a versioned row requires `If-Match` with that strong tag; missing
  or `*` is `428`; stale is `412`. Comparison is strong (RFC 9110). `ApiClient` carries the tag
  automatically from the DTO it last read.
- A row that does not exist yet but has a readable default (a setting, ADR 0011) reports
  `Version` 0 and `ETag: "0"`, and its first write is `If-Match: "0"`.
- `Cache-Control: private, no-cache` on data; never `no-store`, never `max-age`.

### 4. Idempotency: every write is repeat-safe by its own state

There is no `Idempotency-Key` header and no table of recorded calls. Each write that a client may
have to repeat is safe because of state the model already holds:

| Call | What makes a repeat safe |
|---|---|
| `result` (request or flight set) | the `attempt` token, below |
| `complete` | the search's stage for the run: a search already *Aggregated* returns the same outcome and stores no second digest |
| `digests/send` | it only ever sends *Pending* deliveries (ADR 0006); a repeat finds none |
| any `PUT` / `PATCH` / `DELETE` | `If-Match` (§3): a repeat of an applied change is `412` |
| create a search | the name is unique per owner (ADR 0004): a repeat is `409 search.name_taken` |
| create a recipient | the address is unique per owner (ADR 0006): a repeat is `409` |

- **The scraper's `result` uses its `attempt` token.** `claim` mints an opaque `Guid` attempt and
  stores it on the request (or flight-set) row; `result` carries it in the body (DESIGN §5). The
  row is the record: its `Attempt`, `Status` and a `ResultSha256` column answer a repeat — the same
  body gets the first outcome again, a different body is `422` — so a large `rooms[]` body is never
  stored twice. A `result` whose attempt is expired or superseded is `409 run.attempt_superseded`
  and changes nothing (ADR 0003).
- **The check and the write are one `SaveChanges`** under the retrying execution strategy
  (ADR 0001 §1); no transaction is wrapped by hand.
- **Ids are minted by the API**; a client never supplies the id of a row it is creating.
- `claim`, `POST /runs`, `abort` and `finish` are deliberately **not** repeat-safe; ADR 0010 §7
  says how `ApiClient` retries them, and which calls it may repeat blindly.

### 5. Paging: keyset, opaque cursors

- List endpoints take `after` / `before` (opaque cursors), `sort` (field name, `-` prefix for
  descending; a cursor is bound to its sort and refused under another with `400`), and `size`
  (default 25, cap 200; the flight-options endpoint takes its size from
  `results.flight_options_page_size`, ADR 0011, price ascending). They return `items`,
  `nextCursor`, `previousCursor`, `firstItemIndex` and `totalCount`.
- **The cursor carries the position and the count.** The total is counted once, on the request
  without a cursor, and travels inside the opaque cursor with the index of the page's first item;
  later pages return both without a second `COUNT`. A list that changed underneath shows a slightly
  stale total until it is reloaded, which is acceptable for a label.
- Every sortable field is backed by an index ending in the row id. The UI shows "Showing 26–50 of
  62" with Previous/Next; no page numbers.

### 6. Compression, time, naming

- Brotli/gzip on data responses; **never** on authentication endpoints (BREACH).
- Every instant is `DateTimeOffset` in UTC, ISO 8601; the client renders in the user's zone.
- JSON is camelCase; DTOs are records in `Contracts`; enums are strings.

## Not chosen

- *409 for every conflict.* Conflates "someone else changed it" (412, reload and retry) with "that
  name is taken" (409, choose another); the client's recovery differs.
- *Offset paging with page numbers.* Simple, but unstable under inserts and expensive at depth;
  keyset gives stable Previous/Next and the mockups never show "page N of M".
- *Retries with nothing guarding them.* The scraper's home connection drops mid-request often
  enough that a duplicated `result` is a matter of when; the attempt token is the guard.
- *An `Idempotency-Key` header with an `IdempotencyRecord` table* (the wms shape, and this ADR's
  first draft). A table, a purge and a stored response body per call, to protect three calls that
  the model's own state already protects — and a write on every such call against a database
  billed per second awake.

## Consequences

- DESIGN §8 changes: stale edits are `412`/`428`, not `409`; `409` is reserved for uniqueness and
  state conflicts. The model gains a `ResultSha256` column on `SearchRequest` and `FlightSet`;
  `Attempt` on both is an opaque `Guid`. There is no idempotency table.
- ADR 0004 item 9 no longer offers client-minted ids as a route to idempotent creates.
- `ErrorCode`, `Permission` and `SettingKey<T>` share one typed-key base in `Core`.
- `ApiClient` is the only place that knows about ETags, attempt tokens and cursors; screens see
  typed results and `ConflictException` / `PreconditionFailedException`.
- The conventions document, the error-code catalogue (`docs/api/errors.md`) and the OpenAPI
  document are reviewed together in phase 4.

# ADR 0010 — API client: hand-written over a committed, frozen OpenAPI contract

- **Status:** Accepted, 2026-11-20 (proposed 2026-11-06; amended on acceptance: the retry rule for
  calls that are not idempotent, one way to bind a method to an operation; later the same day: the
  version check of ADR 0002 is kept and specified, replacing an "accepted risk" note that
  contradicted it, and the retry rule no longer mentions an idempotency header)
- **Scope:** `Wolfgang.Barefoot.ApiClient`, `Contracts`, `Api` (OpenAPI document), CI

## Context

`ApiClient` implements the Core interfaces over HTTP (ADR 0002) and is the only way any client —
web, scraper, admin tool, tests — talks to the API. The question is whether the client is
**generated** from the API's OpenAPI document or **written by hand**, and how the contract between
API and client is kept honest either way.

The sibling project `Chris-Wolfgang/wms` chose Kiota generation (its ADR 0004): the committed
OpenAPI document is the source of truth, generated code is committed under `Generated/` and
regenerated in the same PR as the API change, and the client depends only on the Kiota runtime.
Its reasons were Native AOT (reflection-free serialization), many consumers (several Blazor
consoles, an Android app) and a spec-driven contract. Barefoot has one consumer family (the
`ApiClient` assembly), no AOT requirement, and a hard requirement that the client *implement Core
interfaces* — which a generated request-builder API does not do, so adapters would be needed on
top of it either way.

## Decision

1. **`ApiClient` is hand-written**: one typed `HttpClient` wrapper per Core interface, using the
   `Contracts` records directly (the same assembly the API serializes, so shapes cannot drift) and
   `System.Text.Json` source-generated context shared with the API.
2. **The OpenAPI document is still the contract.** The API serves it (`Microsoft.AspNetCore.OpenApi`),
   a copy is committed at `docs/api/openapi-v{n}.json`, and a test fails when the served document
   differs from the committed one (regenerate with an environment flag, commit in the same PR).
3. **Path versioning, additive after freeze.** Every route lives under the version prefix; DESIGN
   and the other ADRs write routes without it for brevity. `/api/v0/…` is unstable and may break with a
   `breaking` changelog fragment; `v1` freezes at 1.0 and changes only additively. `docs/api/frozen.txt`
   lists the frozen versions; an `openapi-diff` CI job fails a PR that changes a frozen version and
   only reports the diff for the others. Clients ignore unknown fields.
4. **Coverage test:** every `operationId` in the committed document has exactly one `ApiClient`
   method, bound by an `[Operation("…")]` attribute — the only binding; there is no naming
   convention beside it — and every `ApiClient` method names an operation that exists. Dead or
   missing client code fails the build.
5. **Contract tests** (`WebApplicationFactory` + SQLite) drive the real `ApiClient` end to end;
   they are the only integration suite and the end-to-end gate (DESIGN §14).
6. **Entry point:** `ApiClient.Create(HttpClient, ITokenProvider)`; the web host passes a
   per-user token provider, the unattended hosts a client-credentials provider. The client applies
   the retry posture of ADR 0001 §2 (readiness `503`s are retried within the same budget) and the
   ETag / idempotency conventions of ADR 0012 automatically.
7. **What is retried.** A call is retried automatically only when a repeat is safe:
   - `GET`, any call carrying `If-Match` or an `attempt` token, and the two `POST`s that are
     repeat-safe by state (`complete`, `digests/send`; ADR 0012 §4): on a connection failure, a
     timeout or a readiness `503`.
   - Any other `POST` (`claim`, `POST /runs`, `abort`, `finish`, a create): only when the request
     provably never reached the API — the connection could not be opened, or the answer was a
     readiness `503`. After a timeout or a dropped response it is **not** repeated; the caller gets
     the failure. A blind retry of `claim` would lease a second request and strand the first until
     its lease expired. The scraper recovers through the run itself: `POST /runs/resume`, then
     `claim` again. A create that did land answers the user's own retry with `409`.
8. **Version check, both directions.** The contract carries a **revision**: an integer constant in
   `Contracts`, raised by one with every breaking change to `v0`, and written into the committed
   OpenAPI document; the `openapi-diff` job fails a PR whose `v0` diff is breaking and leaves the
   revision unchanged.
   - `GET /version` is anonymous and never touches the database. It returns the API's build
     version, the contract version (`v0`) and the revision.
   - `ApiClient` compares that revision with the one it was compiled against, before the first real
     call. **During `v0` the two must be equal.** An API with a lower revision is *too old* ("The
     API is older than this tool. Deploy the API."); a higher one is *too new* ("This tool is older
     than the API. Update it.").
   - `bf-scraper` and `bf` print that sentence and exit `VersionMismatch` (5, ADR 0007); the
     scheduled job stops on it. The web host shows it as a full-page state (ADR 0008 §11).
   - **After `v1` freezes** the contract changes only additively, so an older client keeps working
     against a newer API: only *too old* (an API whose revision is below the client's) fails.

## Not chosen

- *Kiota generation + hand-written adapters implementing the Core interfaces.* Keeps the spec in
  the driver's seat, but doubles the client surface (generated builders plus adapters) and adds a
  runtime dependency, for a benefit (AOT, many heterogeneous consumers) barefoot does not need.
  The spec-driven guarantee is kept by 2–4 instead. Revisit if a second consumer language or a
  WebAssembly/AOT client appears.
- *NSwag / Refit.* Reflection-based clients with their own serializer settings; the contract check
  would have to be bolted on anyway.
- *No committed OpenAPI document.* Nothing then stops an accidental breaking change from shipping.

## Consequences

- One place to add an endpoint: the minimal-API map, the Core interface, the `ApiClient` method;
  the coverage test and the committed document catch a missed third.
- The `Contracts` assembly is the client's only shared dependency besides `Core`.
- The OpenAPI document becomes the artefact reviewers read for API changes; PRs that touch it carry
  the document diff.
- **Version skew is detected, not discovered.** The scraper and `bf` run on the home machine and
  can be older — or, mid-release, newer — than the deployed API. Item 8 turns that into one clear
  message and exit code at start, instead of a `400` or `404` from whichever call changed. A
  `breaking` change to `v0` still ships with an updated scraper in the same release; the check is
  what catches the time it does not.

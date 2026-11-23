# Barefoot — Design

**2026-11-06.** Barefoot is a new application, written from scratch in this repository under the
`Wolfgang.Barefoot` namespace. It replaces an earlier single-machine tool (sandals-search, 2026-07,
kept read-only on `Chris-Wolfgang/sandals-search` `archive/v1-wpf`). Nothing is imported from it:
its code, migrations and data stay where they are. What carries over is **knowledge** — how the
booking site behaves, which designs worked, which bugs to design out — and that knowledge is cited
where it shaped a decision. The reasoning for each decision is in `docs/adr/`; this document is the
spec that results. Nothing described here is built yet; app code starts when Chris says so.

---

## 1. What it is

The Sandals / Beaches booking site searches well when you know the resort, departure airport and
dates — and is tedious when any of those are flexible. Barefoot lets a user save a **search**
(resorts × departure airports × a flexible check-in window × nights × occupancy), expands it into
every individual lookup, runs those lookups against the public booking engine in a real browser
(§3), stores **full-fidelity** results (every room option, every flight option), keeps history so
prices can be compared **over time**, runs unattended about twice a week, and e-mails a **digest**
per search — including filtered views the site cannot express.

Several unrelated users share one installation. Each sees only their own searches and results;
identical lookups wanted by several users are performed **once per run**; the UI is hosted and
reachable from anywhere, and the digests are read on phones.

Vocabulary (ADR 0008): the UI noun is **Search** (code: `SearchMatrix`); the individual scrapes are
**lookups**; a run's outcome for a search is *Complete / In progress / Incomplete / Failed*.

## 2. Architecture

Everything reaches the database through one Web API. The API hosts the business logic and the data
access; every other process is a client of it. One exception: the maintenance tool, which owns the
schema and connects directly with a DDL identity (ADR 0002, ADR 0007).

```
 home machine                              Azure                                   anywhere
 ┌─────────────────────┐   HTTPS + token   ┌──────────────────────────┐            ┌────────────┐
 │ bf-scraper          │ ────────────────► │ Wolfgang.Barefoot.Api    │ ◄────────  │ Web UI     │
 │  Playwright/Chromium│                   │  Business (BLL)          │  OIDC user │ (Blazor)   │
 │  residential IP     │                   │  Data (DAL, EF Core)     │            └────────────┘
 └─────────────────────┘                   │  Entra JWT validation    │            ┌────────────┐
 ┌─────────────────────┐   HTTPS + token   │  audit: who did what     │ ◄────────  │ bf         │
 │ bf-maint            │ ─┐                └────────────┬─────────────┘            │            │
 │  migrate, repair    │  │ direct, DDL identity        │ managed identity         └────────────┘
 └─────────────────────┘  └───────────────► Azure SQL Database (free offer, serverless, auto-pause)
```

| Project | Layer | Contents |
|---|---|---|
| `Wolfgang.Barefoot.Core` | Domain + contracts | entities, the service interfaces every client consumes, pure services (window resolver, expander, validators, digest renderer), typed keys (`ErrorCode`, `Permission`, `SettingKey<T>`) |
| `Wolfgang.Barefoot.Contracts` | DTOs | request/response records the Core interfaces take and return, so the API and `ApiClient` share one set; the contract revision |
| `Wolfgang.Barefoot.Data` | **DAL** | `DbContext`, configurations, the single `InitialCreate` migration, persistence queries/commands behind Core interfaces |
| `Wolfgang.Barefoot.Business` | **BLL** | run lifecycle, search editing + authorization rules, catalog snapshot diffing, digest building + delivery, vocabulary rules, settings |
| `Wolfgang.Barefoot.Api` | host | ASP.NET Core minimal API over Business + Data; Entra JWT validation; `HttpContextAuditUserProvider`; readiness (ADR 0001); problem details, ETags, repeat-safe writes, paging (ADR 0012) |
| `Wolfgang.Barefoot.ApiClient` | client | HTTP implementations of the same Core interfaces, used by every client (ADR 0010) |
| `Wolfgang.Barefoot.Obe` | adapter | Playwright booking, flight and catalog clients, selectors, parsers ("online booking engine"; informed by the earlier tool's `docs/API-NOTES.md`, redacted) |
| `Wolfgang.Barefoot.Scraper` (`bf-scraper`) | host | `Obe` + `ApiClient`; the only process with a browser and a home IP |
| `Wolfgang.Barefoot.Admin` (`bf`) | host | `ApiClient` only; scriptable "API, do X" steps; runs anywhere the API is reachable |
| `Wolfgang.Barefoot.Maintenance` (`bf-maint`) | host | `Data` only; `migrate`, `verify-schema`, `grant-roles`, `assign-owner`; the one direct-DB client |
| `Wolfgang.Barefoot.Presentation` | UI logic | framework-free screen logic (`SearchDraft`, pickers, chart scaling, paging), unit-tested without a UI framework |
| `Wolfgang.Barefoot.Web` | host | Blazor Web App (Fluent UI Blazor) over `Presentation` + `ApiClient`; user sign-in |

**Dependency rules**, enforced by project references *and* by architecture tests (§14):
`Scraper`, `Admin` and `Web` never reference `Data` or any EF package; `Admin` never references
`Obe`; `Maintenance` never references `ApiClient` or `Obe`; `Api` never references `Obe`;
`Core`, `Contracts` and `Presentation` reference no framework; `Contracts` references no project
and `Core` references only `Contracts`. The Core interfaces are the API surface and take and
return the `Contracts` records (ADR 0002 §4):
`Data`/`Business` implement them server-side, `ApiClient` implements them over HTTP, and
`Presentation` does not know which it has.

**Registration is explicit** (no assembly scanning); background work is a table, not a queue
(§5): the scraper leases rows from the API.

**Version check** (ADR 0002, ADR 0010 §8). The contract has a revision number, raised with every
breaking change. `GET /version` is anonymous, never touches the database, and returns the API's
build, contract version and revision. Every client compares it with the revision it was built
against before its first real call: during `v0` the two must be equal, so a client detects an API
that is too old *and* one that is too new. `bf-scraper` and `bf` say which side to update and exit
5; the web host shows a full-page state. After `v1` freezes only a too-old API fails.

## 3. Booking-engine integration

`Obe` drives the booking UI in Chromium through Playwright and reads the rendered room and flight
cards. The site knowledge — page flow, selectors, the price-text formats and their traps — is the
one thing taken from the earlier tool's notes, redacted per sandals-search#261:
[`docs/API-NOTES.md`](API-NOTES.md). Politeness: sequential, a delay
(~2.5 s) plus jitter that the run hands to the worker (`run.politeness_delay_ms`,
`run.politeness_jitter_ms`; ADR 0011), one lookup at a time. Earliest searchable check-in ≈ today +
`run.min_lead_days`. Prices are what the public site shows for the given occupancy, taxes included.

Price text is parsed with anchored, `NonBacktracking`, time-limited regular expressions and the
parsers are fixture-tested against captured markup — the earlier tool's price-text bugs
(sandals-search#225 family) are designed out, not patched.

## 4. Searches

A search has two halves:

- **What to search** (every combination is downloaded): brand (Sandals / Beaches), resorts,
  departure airports, nights, check-in window, occupancy (adults; children and infants only for
  Beaches), include-flights flag, cabin.
- **Digest filters** (what the digest shows): room level / view / type / must-have features, max
  total price, max drive to resort (derived from the resort's distance to its gateway airport, so it
  applies with or without flights), max flight time including layovers and max stops (flights
  only). **Filters apply at aggregation time only — capture always downloads everything.**

Lifecycle: Draft → Active → Inactive (results retained); Delete only for Drafts that never ran.
Active searches carry a **run order** (stored 1–99, edited as Low / Below normal / Normal / Above
normal / High = 1 / 25 / 50 / 75 / 99; ties run in creation order).

Every search has an **owner** (`OwnerId`, the user's Entra object id). Editors see and edit only
their own searches; Admins see all. Users are unrelated to one another: nothing a user sees reveals
that another user exists. A search **name is unique per owner** (case-insensitive, trimmed).
Digests go to the owner's sign-in address; an Admin may add further recipients to the owner's
address book and choose which of them each search mails; self-service "add my spouse" with address
verification is phase 2 (ADR 0006). An Admin can create a search for another user, but only for
one who has signed in once: the owner picker lists `AppUser` rows (ADR 0004 §8).

**Check-in windows.** *Recurring* (every year): a month/day pair, resolved at run time —
`effectiveStart = max(resolvedStart, today + minLeadDays)`, rolling to next year when
`today + minLead > resolvedEnd`; year-wrapping windows are legal. *One-time*: explicit first and
last check-in dates; the search deactivates itself once the window passes and raises an Open issue
(§12).

## 5. Run lifecycle (ADR 0003)

The API owns the lifecycle; the scraper pulls work. Decisions about *data* are the API's, decisions
about *the site* are the scraper's.

| API / Business (data decisions) | Scraper (site decisions) |
|---|---|
| create the run; at most one in progress, enforced by a filtered unique index on `Runs(Status)` | Playwright, headed/headless |
| explode every active search; **dedupe identical requests across searches** (§8) | politeness delay + jitter, per-item retries |
| lease requests to a worker; expire leases from a dead worker | the circuit-breaker *decision* |
| persist results and flight sets; discover vocabulary | time-zone arithmetic on scraped times |
| advance stages; aggregate; store digests; derive run status; resume semantics | — |

```
POST /runs                      {trigger}              → run + worker {politenessDelayMs, politenessJitterMs}; 409 if one is in progress
POST /runs/resume                                      → the incomplete run + worker {…}, or 404
POST /runs/{id}/requests/claim  {worker}               → next Pending request (run-order) + attempt + leaseExpiresUtc, or "none left"
POST /runs/{id}/requests/{r}/result {attempt, rooms[] | error}   → idempotent on attempt (ADR 0012)
POST /runs/{id}/flight-sets/claim   {worker}           → next Pending flight set + attempt + leaseExpiresUtc, or "none left"
POST /runs/{id}/flight-sets/{f}/result {attempt, options[] | error}
POST /runs/{id}/searches/{s}/complete                  → Downloaded → Aggregated; stores the digest (no send)
POST /runs/{id}/abort           {reason}               → breaker tripped; the rest waits for resume
POST /runs/{id}/finish                                 → derives Complete | Incomplete | Failed
POST /runs/{id}/digests/send                           → mails every Pending digest (bf); 200 with sent/failed counts
```

Scraper loop: `POST /runs` (or `/resume`) → claim → scrape → result … until none left → flight
sets the same way → `complete` each search whose work is done → `finish`. A tripped breaker calls
`abort` and exits 3. The API sets every lease (`run.lease_seconds`, 5 min by default; the worker
does not choose it) and hands the worker its politeness values with the run, so the scraper reads
no settings of its own (ADR 0011). A crashed worker's leases expire and its requests return to Pending;
a late duplicate `result` is ignored by its `attempt` token. No call carries an idempotency header:
each write is safe to repeat because of its own state (ADR 0012 §4). A run knows its complete work
list at creation, so progress is countable and the UI can show a run in flight.

**Semiweekly job** (a scheduled task on the home machine, gated on the shared `ExitCode` enum,
ADR 0007 — stop on 5, 7, 8 or 9; continue past 2 and 3). `bf-scraper` runs as the `Scraper`
service principal and `bf send-digests` as `DigestSender` (ADR 0005):
```
bf-scraper refresh-airports
bf-scraper refresh-resorts
bf-scraper run            # or: bf-scraper run --resume
bf   send-digests
```

Catalog refresh posts a **snapshot** of what the site shows; the API diffs it against the catalog,
applies the add/update/deactivate rules and a safety rule so a bad scrape cannot take every resort
offline at once, and audits the changes as the scraper's identity. **A code repeated within one
snapshot never fails the refresh**: identical entries collapse to one; entries that disagree leave
that one resort (or airport) untouched and are named in the response's `warnings`, which the
scraper logs and reports with exit code 2 (partial). Every other entry is applied.

## 6. Flights

- Airfare is bookable only ~6 months out; beyond the horizon room-only prices are stored and airfare
  fills in once in range. `airfarePrice = combined total − room price` per option.
- A **FlightSet** is keyed per run by (origin airport, gateway airport, depart date, return date,
  cabin, travellers) — not by resort; resorts on one island share a gateway. Flight sets are created
  at explode time as work items and deduped across every search in the run.
- Full fidelity: FlightOption → FlightDirection ×2 → FlightLeg.
- A search item links to **one flight set per departure airport** through `SearchItemFlightSet`.
- Flight options are served **paged, ten at a time** (`results.flight_options_page_size`,
  ADR 0011), **price ascending**, from a per-flight-set
  endpoint (§12 Results); the digest carries only the cheapest per departure airport.

## 7. Reference data

- **Resort** = permanent identity row (code, name, **short name** for lists and digests, brand,
  island, gateway airport FK, distance to gateway).
- **ResortSpan** = effective-dated status with `1900-01-01` / `9999-12-31` sentinels; machine-
  maintained from scrape diffs.
- **Vocabularies** (RoomLevel/View/Type/Feature) are **discovered** from results, never authored.
  A value not seen in *n* runs is greyed in the editor ("not seen in N runs"), never deleted;
  `bf prune-vocab` or an Admin action is a later feature.
- **RoomCategory**: per-resort code + name, first/last seen; price-over-time identity =
  (resort, check-in date, room category).
- **Airports** carry their IANA time zone (`Airport.TimeZoneId`, nullable). The site gives only a
  code and a name, so the zone comes from a **code → zone lookup bundled with the API** and is
  filled in when a catalog snapshot is applied. An airport the lookup does not know is saved with
  no zone and raises an Admin Open issue (§12) that clears when the Admin sets the zone on the
  Reference data page; until then its flight times are shown as the site gave them, unconverted.
  *Future, not v1:* missing airport data is filled automatically — the console app asks the API
  what is missing and pulls it, or the API fetches it when the airport is saved.

## 8. Data model (ADR 0004)

```
AppUser (Oid GUID PK, DisplayName, Email, LastSeenUtc, DisclaimerVersionAccepted/At, TimeZoneId,
         DefaultAdults/Children/Infants, SessionsValidAfterUtc, Version)   seeded: 0000…0000 "Unassigned"
SearchMatrix (+ OwnerId → AppUser, Name, NormalizedName, Brand, RunOrder, Version)   UNIQUE (OwnerId, NormalizedName); audited
Run (filtered UNIQUE on Status WHERE Status = InProgress)        the run lock
MatrixRun (per search per run; Stage Exploded → Downloaded → Aggregated → Emailed)
SearchRequest (Run, Resort, CheckIn, CheckOut, Adults, Children, Infants,    ← the deduped unit of work
               RunOrder, Status, Tries, LastError, CapturedUtc, LeasedBy, LeaseExpiresUtc,
               Attempt (Guid, minted by claim), ResultSha256, Version)
               UNIQUE (Run, Resort, CheckIn, CheckOut, Adults, Children, Infants)
SearchItem (MatrixRun, SearchRequest, Cabin)   UNIQUE (MatrixRun, SearchRequest)   ← one per search wanting it
RoomResult (SearchRequest, …)                                     scraped once
SearchItemFlightSet (SearchItem, FlightSet)                       one per departure airport
FlightSet (+ Status, LeasedBy, LeaseExpiresUtc, Attempt, ResultSha256, LastError, Version; CapturedUtc nullable)
Recipient (OwnerId → AppUser, Address, DisplayName, Version)   UNIQUE (OwnerId, Address); audited   ← the owner's address book
SearchRecipient (SearchMatrix, Recipient)   UNIQUE (SearchMatrix, Recipient)        ← which recipients a search mails
DigestDelivery (MatrixRun, RecipientUserId → AppUser NULL for an extra, RecipientAddress (a copy),
                Subject, HtmlBody, TextBody, Status, Attempts, LastError, CreatedUtc, SentUtc)
OpenIssue (UserId → AppUser, Kind, Subject, ResolvesWhen, AcknowledgedUtc, ResolvedUtc)
Setting (Key PK, Value text, UpdatedUtc, Version)                 ADR 0011; audited
Resort, ResortSpan, Airport, RoomCategory, vocabularies (+ Version)   audited
```

- **Dedupe is per run.** Identical `(resort, dates, occupancy)` across searches is one
  `SearchRequest` with one set of `RoomResult`s; each search links through its own `SearchItem`.
  `Cabin` is not in the key — it affects flights, not rooms. Across runs nothing is deduped: prices
  change, which is the point of searching twice a week.
- **Visibility is a join**, not a column: a user sees a result because one of *their* searches links
  to its request (`RoomResult → SearchRequest → SearchItem → MatrixRun → SearchMatrix.OwnerId`).
  One predicate, applied in the BLL, scopes searches, items, results, digests and price history.
- **Item status is per request.** If a scrape fails, it fails for every search that wanted it, and
  each digest says so.
- **Keys** are `Guid`s (`uniqueidentifier`), sequential where the row is hot (results, requests);
  `OwnerId` is the Entra object id parsed once at the auth boundary.
- **The "Unassigned" user** (`Guid.Empty`) owns whatever is created before the application is
  connected to Entra — until then there is one developer and no real object id, so local runs act
  as this user. Once sign-in works, `bf-maint assign-owner <oid>` moves every row it owns to that
  user and is run once (ADR 0004 §8). Nothing is imported from the earlier tool; this is not an
  import path.
- **Concurrency:** a portable `Version` column (`long`, concurrency token, incremented by the
  context) on user-edited and claimable rows — SQL Server `rowversion` is not available on the
  SQLite test provider. `Version` is also the row's ETag; a stale edit is `412`, an edit without a
  precondition `428` (ADR 0012). Uniqueness violations (search name, run in progress) are `409`.
- **One migration.** The model ships as one `InitialCreate`; there is no earlier schema and no data
  to carry. Every later migration has a working `Down` and CI runs up → down → up (ADR 0001 §4).
- **Audit:** `Wolfgang.AuditTrail` audits configuration and reference entities; `user_id` names
  the calling application (web, scraper, `bf`) and `on_behalf_of` the signed-in person's `oid`
  when there is one (ADR 0005). Results and execution tables are `[NotAudited]`.
- **Conventions:** snake_case names with `pk_/ak_/fk_/ix_/ux_` prefixes, `Restrict` on every FK, a
  model-convention verifier asserted empty by a test on SQLite, every index cited by the query it
  serves; `DateTimeOffset` in UTC everywhere, `TimeProvider` injected, `DateTimeOffset.Now/UtcNow`
  banned.

## 9. Aggregation & e-mail (ADR 0006)

- **Aggregate on `complete`, store, send later.** `complete` builds the per-search digest (HTML +
  plain text) into a `DigestDelivery` row, status *Pending*, and sends nothing.
  `POST /runs/{id}/digests/send` — the last job step, via `bf` — mails the stored bodies and
  records *Sent* / *Failed* + attempts + error, answering `200` with the counts. Everyone's mail
  goes out together, after the run is known-good, and the stored body is exactly what was sent. A
  failed delivery raises an Open issue for the search's owner, whichever address failed ("We
  couldn't e-mail your digest to …", resolves when a delivery succeeds).
- **Recipients:** the owner's sign-in address, plus the extras chosen for that search from the
  owner's address book (`Recipient`, linked by `SearchRecipient`; §8). An Admin maintains the
  address book — an address is changed once and every search using it follows — and an owner has
  at most `digest.max_recipients_per_owner` (5). Verified self-service extras are phase 2. One
  message per recipient, never CC/BCC across users; no cross-user content in any template;
  addresses and bodies are never logged above Debug.
- **Sender:** interim — a dedicated mailbox owned by the Admin (not the Admin's personal mailbox),
  credentials in Container Apps secrets / Key Vault; later Azure Communication Services or a
  transactional provider on an owned domain with SPF/DKIM. One `ISmtpClient` implementation either
  way; a file-sink implementation for local runs.
- **Views:** Admin — every delivery and its status (`GET /digests`); user — "my digests", rendered
  from the stored HTML (`GET /digests/mine`, paged, filtered by search, date range, text and
  undelivered). A user may **resend** any of their own digests to any of its recipients; an Admin
  may resend anyone's. A resend mails the stored body again, unchanged.
- **Failure notices** go to the Admin only; a user's digest says "3 of 14 lookups did not
  complete", never why.
- Content: §1 summary per resort, §2 resolved criteria incl. offline-resort notices, §3 detail
  (cheapest → dearest, with amenities and cheapest flight per departure airport), plus deep links
  to the web UI.
- **Mobile-first markup — the digest is read on a phone.** Single column, 600 px max, 16 px body,
  generous tap targets. §3 is **one card per room** (price large on the first line; room · level ·
  view · amenities on the second; check-in as a chip) under resort sections whose header carries the
  section's cheapest price, so the summary scans without opening anything. Table-based layout with
  inline CSS, dark-mode-safe colours, a plain-text part always, and an "Open in <Name>" deep link
  per resort and per digest that lands on the phone-contract Results screen with search and run
  pre-selected. Acceptance: renders in Gmail (iOS + Android), Apple Mail, Outlook mobile and Outlook
  desktop; a fixture test pins the markup.

## 10. Identity & authorization (ADR 0005)

- **Entra ID**, the tenant the Azure subscription already has; app registration with *assignment
  required*; the dozen users invited as **B2B guests** (Microsoft, Google or one-time-passcode
  sign-in). No sign-up page, no stored passwords.
- **App roles:** `Admin` (everything), `Editor` (create/edit/deactivate own searches; see own
  results and own searches' run outcomes), `Viewer` (read own), and two service-principal roles:
  `Scraper` (`bf-scraper`: execute runs over every user's active searches and write catalog
  snapshots; it cannot list or read searches or results) and `DigestSender` (the scheduled
  `bf send-digests`: send pending digests and read delivery status, nothing else). `bf` run by hand
  signs the user in and acts with their own roles. Runs themselves are Admin-only objects.
- The API validates Entra JWTs itself (one gate for UI, scraper, admin) and **never redirects**:
  401/403 are problem details. The web host signs users in with OIDC (cookie on the web host), calls
  the API with the user's token acquired server-side — the token never reaches browser script — so
  the API, and the audit trail, see the person.
- **Every endpoint declares a permission or `AllowAnonymous()` with a comment saying why**; an
  architecture test fails any endpoint that does neither. Role changes and disables take effect
  before token expiry: the API reads the `AppUser` row per request (short cache) and honours a
  per-user *sessions valid after* instant, which the Admin sets with **Sign out everywhere** on the
  Users page after changing a role in Entra.
- **Database identities, no passwords:** API managed identity → `db_datareader` +
  `db_datawriter`; maintenance → DDL via the operator's Entra user locally or the job's managed
  identity in the pipeline (`bf-maint grant-roles` is idempotent and part of `migrate`).
- Security headers on the web host (CSP `default-src 'self'`, `frame-ancestors 'none'`, nosniff,
  referrer policy) with an integration test; the CORS allow-list and the session lifetimes are
  bootstrap configuration on the API and the web host, not registry settings (ADR 0011 §6).

## 11. Database (ADR 0001)

Azure SQL Database **free offer**, serverless, auto-pause 15 min, exhaustion behaviour *pause until
next month*. The API treats a sleeping database as normal: `Connect Timeout=90`, EF retry on the
resume errors, an observable **readiness** state (`Unknown → Waking → Ready | Unavailable |
QuotaExhausted | MissingMigration | NewerSchema`) that the UI renders full-page ("Warming up.
Please wait…" after 2 s; a quota-exhausted screen naming the reset instant; "Missing migration: …
Run `bf-maint migrate`"; "The database is newer than this build"). The API *always* starts and
gates its data endpoints on readiness; a schema **newer** than the build is one more readiness
state, in which the API reads and writes nothing. `Unavailable` is re-probed when a request
arrives, at most every 30 s, never on a timer. SQLite is the test provider; the Data layer has no
raw SQL and no provider-specific column types outside the migration.

There is **no manual pause** for a serverless database; 15 minutes of inactivity is the only
trigger and the floor. At twice a week the idle tails cost under 10 000 vCore-seconds a month of
the 100 000 allowance. What *is* worth a rule: **no client stays connected** (SSMS Object Explorer,
Azure Data Studio, a forgotten `sqlcmd`) — an open session holds the database awake indefinitely;
the maintenance tool connects, works and disconnects. The Admin dashboard shows the live `status` so
an `Online` at an odd hour is visible.

## 12. UI (ADR 0008)

**Name.** The product name is a display string (placeholder "Barefoot Vacation"); wherever it appears it reads
"<Name> — for Sandals & Beaches resorts" in plain words, with the not-affiliated statement on the sign-in page
and in every page footer (ADR 0009). No logo, type or colour of theirs; the rail mark is a neutral glyph on the
accent, which is the Sandals light blue (`#05b2e8`, lifted to `#2cc3f0` in dark mode for contrast).

**Host and library.** A Blazor Web App (`Wolfgang.Barefoot.Web`, `InteractiveServer` render mode
globally) built on **Fluent UI Blazor** — accessible by default, a neutral `FluentDesignTheme` with
light/dark, a data grid that sorts, pages and virtualises. Charts are rendered as **our own SVG**;
no chart dependency. Two containers (API, Web), both scale to zero.

**Components own their state.** No MVVM layer: the framework-free logic lives in `Presentation` —
`SearchDraft` (the editor's load/save mapping, window-mode rules, picker and chip logic, fan-out
wiring), `ResortPickItem`, `IslandGroup`, `FilterPickItem`, `ChartScaler`, `PriceHistoryRange`, a
paging helper — unit-tested without any UI framework. Screens call the Core interfaces through
`ApiClient` directly. Every routable page is under the gated layout or carries an `[Authorize]`
policy; a test checks it.

**First sign-in and attention.** A **disclaimer** (not affiliated, prices are not quotes, scheduled
lookups, who can see your data, personal use) is accepted once per user per disclaimer version
(`AppUser.DisclaimerVersionAccepted`); Decline signs out. After it, a **needs-your-attention**
pop-up lists new Open issues; acknowledging only stops the pop-up — an issue stays on the
Dashboard until its `ResolvesWhen` condition clears (digest delivery failed → resolves when a
delivery succeeds; one-time window passed → resolves when a new window is set; resort offline →
resolves when the site lists it again; Admin-only: deliveries failed for all users; an airport
with no time zone → resolves when one is set).

**Information architecture by role.** An Editor's world is their searches; an Admin's world is
the system. Everything an Editor sees is reachable from a search they own.

| | Editor / Viewer | Admin (adds) |
|---|---|---|
| Landing | **Dashboard** — status strip, Open issues card, Recent results (last 10 runs of my searches: run time, outcome pill, lookups done/total, cheapest, vs previous, digest link), biggest movers, latest digest; first sign-in shows one action, "Create your first search" | **System dashboard** — last run (status, duration, lookups, failures, seconds per lookup), **Budget & health** (below) |
| Work | **Searches** (locked list, below) → **Search editor** (below) | Owner column + owner filter; "mine" by default, "all owners" toggle; can open any search |
| Read | **Results** (below) — search + run pickers (runs are Admin objects), digest-filters/everything toggle, paged grid, flights per departure airport | any owner |
| Read | **Price history** — over *my* captures only (v1) | cross-user view over all captures |
| Read | **My digests** — every digest sent to me, found, filtered and paged, rendered from the stored body, linking to its results; Resend per recipient | **Deliveries** — every delivery, status, attempts, error, resend |
| Me | **Profile** — name/e-mail (from sign-in, read-only), time zone, default occupancy; my extra recipients (read-only until self-service) | — |
| Admin | — | **Runs** (history + live progress: requests and flight sets done/total, per-search stage, abort reasons), **Reference data** (resorts + spans, airports + time zones, vocab with the future prune action), **Users** (role, search count, last sign-in, the user's extra recipients, Sign out everywhere; inviting stays in Entra), **Settings** (settings registry, read/write — ADR 0011) |

- Admin is also an Editor (own searches). **Viewer** = receives digests and browses results; the
  Admin creates searches *for* them, once they have signed in and accepted the disclaimer — until
  then they are not in the owner picker. Kept for v1 even if unused.
- Runs are invisible to Editors as objects; they see per-search outcomes only ("Tue 06:12 ·
  Complete"), never run ids, breaker reasons or other searches.
- **A failed load never takes the application down.** Every screen has four explicit states —
  Loading, Loaded, Empty, Failed — and Failed renders in place: the problem detail's message, its
  `traceId`, and a Retry. An `ErrorBoundary` in the gated layout wraps the page body, so an
  exception a screen did not expect still leaves the shell and navigation working. Errors are
  never modal dialogs. A `412` on save says the row changed elsewhere and offers Reload; a
  readiness `503` switches to the full-page readiness state. A test per screen drives a failing
  `ApiClient` and asserts the Failed state.
- Readiness states render full-page, before the shell, for every role, from the API's readiness
  endpoint; a version mismatch between the web host and the API (§2) renders the same way.
- **Polling.** A page polls the API every ~30 s **only while its last load showed a run
  `InProgress`**. A page that saw no run in progress does not poll, and nothing else in the UI
  polls: a poll is a query, and a query holds the serverless database awake (§11).
- No per-user Settings page (the Admin's **Settings** page holds the registry; a user's own
  preferences are on Profile); no run history for
  non-admins; no "Refresh now" on Reference data (scraping is `bf-scraper` on the home machine).
- Future version: a first-run tour / wizard.

**Searches list (locked 2026-11-06).** Name (unique per owner) + generated subtitle; window on two
lines with a recurring icon (tooltip "Repeats every year") or an amber *passed* tag; resorts by
short name with airport codes on their own line; last run as date + outcome pill; cheapest; vs
last; status; icon actions edit / deactivate / reactivate / delete with tooltips. No run-order
column (it lives in the editor).

**Search editor (locked 2026-11-06, mockup v32).** Two panes and a fan-out footer at the end of the
form. *What to search*: Name (unique per owner), Brand (Sandals / Beaches — Children and Infants
only for Beaches), Check-in window (recurring month + day, or one-time date pickers), Adults,
Resorts grouped by island with the gateway code, then a flight section — Include flights, Cabin,
Departure airports — that is hidden entirely when flights are off. *Digest filters*: the room
vocabularies (stale values greyed, "not seen in N runs" under the name), Max total price, Max drive
to resort, Max flight time incl. layovers and Max stops (flights only). *Run order* is its own
section. *Recipients* last: a pick-list from the owner's address book, changed by an Admin and
read-only for the owner. The footer shows resorts × dates → room lookups, airports → flight
lookups, minutes per run, and holds Cancel / Save.

**Results (locked 2026-11-06, mockup v37).** One search, one run. Pickers: Search name, Run (capture
date + outcome), Find; scope *Matching my digest filters* / *Everything found*; sort defaults to
cheapest total. One row per room option: resort and check-in, room (level, view), room price
(regular, sale, promo), one button per departure airport with the fare range and option count, and
the total (room + cheapest flight) with the delta against the previous run for the same resort, room
and check-in. Clicking an airport opens that airport alone across the row: the cheapest round trip
with its legs, then "N more" which pages the remaining options **ten at a time, price ascending**,
from a paged flight-options endpoint per flight set. Offline resorts appear as a row. Phone: cards,
one airport panel at a time.

**The remaining screens (locked 2026-11-20).** Specifics are in the mockup README.
*My digests*: found, filtered (search, date range, text, undelivered) and paged; the stored message
beside the list, each recipient with its delivery status and Resend. *Profile*: identity read-only,
time zone, default adults, the address book read-only. *Price history*: one room on one check-in
date across runs — a two-series chart on one dollar axis (total with flights, room alone) with the
same numbers as a table. *Runs*: a live card only while a run is in progress, then paged history
with the breaker's reason under its row. *Deliveries*: every recipient of every digest, Resend and
retry-failed. *Reference data*: Resorts (the short name and drive time are the Admin's to edit;
the site's facts are not), Airports (a missing time zone is set here), Room vocabulary. *Users*:
who has signed in and accepted the terms, each user's address book, Sign out everywhere. *Settings*:
the settings registry with inline validation and reset, and what is running (builds, contract
revision, schema). *Startup states*: Warming up, Quota used up, Missing migration, Newer schema,
Unavailable, Version mismatch — full page, every role.

**Tables never leave their card.** Long text wraps; a note about a row sits under it; on a narrow
window the less important columns are dropped first, and Results drops none. Sideways scrolling
inside the card is the last resort.

**Budget & health (Admin dashboard).** Table-backed cards ship with the dashboard; the Azure
Monitor cards follow once the API's managed identity has *Reader* on the database and
*Monitoring Reader* on the resource group:
database `status` (`Online | Paused`) and `free_amount_remaining` this month with days to reset;
storage used vs 32 GB; API + Web compute vs the Container Apps free grant (Cost Management, lags
8–24 h, shown "as of"); digests sent this month; last run duration / lookups / failures / seconds
per lookup; scraper last seen (last `claim`). `Online` at an odd hour means a session is holding
the database awake.

**Price history scoping.** v1: strict for Editors (own captures), cross-user for Admin — the
"no cross-user signal" rule holds for the dozen. **v2 priority:** a shared *market view* for
everyone — **cost per adult per night** (`TotalPrice ÷ Adults ÷ nights`, derivable, no schema
change), per resort and room level, bucketed by week — so a new user can see "March is cheaper
than February" on day one. Sandals is adults-only, so the figure is exact until Beaches (children)
is searchable; an "adults only" toggle is a Beaches-era item.

**Mobile.** The web app's design target is a laptop/tablet; the **search editor is
desktop-first**. The *phone contract* — the screens a digest links to and that must work on a
phone: sign-in, Dashboard (cards), My digests, Results for one search (cards instead of the grid,
one airport panel at a time); a bottom tab bar for those plus Profile; Admin under "More".
Everything else must merely not break.

**Auth UX.** "Sign in with Microsoft / Google" (Entra does the rest); first sign-in creates the
`AppUser` row, shows the disclaimer, and lands on the empty Dashboard; sign-out and "who am I" in
the header.

Mockups: `docs/mockups/v2/` (current, with a README of locked screens and decisions);
`docs/mockups/v1/` is the earlier tool's mockup, kept for comparison.

## 13. Repository and build phases

**Repository.** `barefoot` was created from `repo-template` (clean history, current template by
construction, the code name in the URL). `main` is the release branch; work lands on **`develop`**
through stacked PRs (`docs/STACKED-PRS.md`, `scripts/restack.ps1`); `pr.yaml`,
`protected-files.yaml`, `codeql.yaml` and `stryker.yaml` run for PRs into `develop` from the first
commit. The earlier tool's open issues were triaged (claude-sessions
`sessions/sandals-search-issue-triage-v2.md`): the ones that still describe a requirement are
re-filed here, per phase, with a *context: sandals-search#N* line; the rest stay there as its record,
and `sandals-search` is archived once this repository ships.

**Phases** (each a stack of small PRs on `develop`, each PR reviewable alone; app code starts only
on an explicit go):

0. **Conventions and gates** — `docs/CODING-CONVENTIONS.md`, `API-CONVENTIONS.md`,
   `DATABASE-CONVENTIONS.md`, `MIGRATE.md`, `SETTINGS.md`, `DELIVERY-PROCESS.md` (adapted from
   `Chris-Wolfgang/wms`, see `docs/notes/2026-11-06-wms-reuse-review.md`); the architecture test
   project with its positive-control fixtures; `report-failure.ps1`, `Generate-ThirdPartyNotices.ps1`,
   the pr-gate / prerelease-review / release skills.
1. **Core + Contracts** — entities, typed keys, the service interfaces, pure services (window
   resolver, expander, validators, digest renderer) with their tests.
2. **Data + Maintenance** — `DbContext`, configurations, the single `InitialCreate`, the
   model-convention verifier; `bf-maint migrate` / `verify-schema` / `grant-roles` /
   `assign-owner` (ADR 0001 §4); CI applies the migration up → down → up on SQL Server.
3. **Business** — run lifecycle, dedupe at explode, visibility predicate, search editing and
   name uniqueness, catalog diffing, digest building, settings registry (ADR 0011).
4. **Api + ApiClient** — the Core interfaces over HTTP; problem details, ETags, repeat-safe
   writes, paging (ADR 0012); Entra JWT validation; readiness; `GET /version` and the client-side
   check (ADR 0010 §8); the committed OpenAPI document and the
   `frozen.txt` diff gate; `WebApplicationFactory` contract tests over SQLite driving the real
   `ApiClient` — the end-to-end gate (ADR 0010).
5. **Obe + Scraper + Admin** — Playwright adapters and fixture-tested parsers; `bf-scraper run`,
   `refresh-*`, `search one`; `bf send-digests`, `digests status`, `search list`, `version`.
6. **Azure** — SQL free offer, Container App for the API (scale to zero), managed identity and
   `grant-roles`, secrets, the two service principals (`Scraper`, `DigestSender`), the firewall
   rule for the machine that runs `bf-maint`, the semiweekly scheduled task.
7. **Web** — Blazor Web App, screen by screen in mockup order: disclaimer + Open issues, Dashboard,
   Searches, Search editor, Results, My digests, Profile, Price history, Admin pages, readiness
   pages; the digest template and its render acceptance.

## 14. Quality bar

95 % line coverage on every `src/` assembly and 100 % on test assemblies; the analyzer suite with
warnings-as-errors; CodeQL + DevSkim + Semgrep + InspectCode; Copilot + user review on every PR;
Stryker per test project; changelog fragments on every PR. **Architecture tests** (each with a
positive-control fixture that proves the scanner can fail): UI and worker projects reference no
data packages; `Core`/`Contracts`/`Presentation` reference no framework; no `DateTime` on a public surface; a
dependency deny-list with reasons; every endpoint declares a permission; every page is gated;
conversion methods live where the conventions say. **API contract tests** (the integration suite
over `WebApplicationFactory` + SQLite, through `ApiClient`) are the end-to-end gate; every client
talks to the API only through `ApiClient`, so the contract is tested once. The committed OpenAPI
document must equal the served one.

**Accessibility** is an acceptance item for every screen in phase 7, not a property assumed from
the component library. The target is WCAG 2.2 AA. A screen is accepted when an automated axe scan
of the rendered page reports no violations in both light and dark themes, every icon-only button
has an accessible name, every input has a visible label, and the screen's main task can be
completed with the keyboard alone. The scan runs in CI over every routable page.

**Logging.** Every host logs through `Microsoft.Extensions.Logging` with structured properties;
no host writes a log line with `Console.Write`. The API and Web containers log to the console,
which Container Apps collects. The three command-line hosts log to **stderr and a rolling file**
under the user's local application data; **stdout carries only the verb's own output**, culture-
invariant, so it stays parseable. An unhandled exception is one line on stderr, the stack trace in
the file only, and an exit code from the shared `ExitCode` enum (ADR 0007). EF Core command logging is at
Warning. Every log entry for an API call carries the `traceId` the problem detail reports.

## 15. Adopted from `wms`

`Chris-Wolfgang/wms` is the sibling product whose engineering conventions were reviewed on
2026-11-06 (`docs/notes/2026-11-06-wms-reuse-review.md`). What barefoot takes, and where it lands:

| From wms | Into barefoot |
|---|---|
| Migrate tool: declarative target, `--status`, offline `--script`, its exit codes (renumbered into the shared `ExitCode` enum, ADR 0007), database-must-exist, newer-schema-never-touched, working `Down` + up→down→up in CI, downgrade data-loss screen | ADR 0001 §4; `bf-maint migrate` (phase 2); `docs/MIGRATE.md` |
| Schema probe `{current, expected, upToDate}` from the same code path as the tool | ADR 0001 §4; `GET /system/schema` |
| Problem details with typed `ErrorCode`; status-code table; `If-Match` 428/412; keyset paging; no compression on auth; UTC `DateTimeOffset` | ADR 0012; `docs/API-CONVENTIONS.md` (phase 0) |
| Path versioning, committed OpenAPI equal to served, `frozen.txt` diff gate | ADR 0010; `docs/API-VERSIONING.md` |
| Every endpoint declares a permission or `AllowAnonymous` with a reason, enforced by a test; grants recomputed per request; cookie on web, token server-side; security headers with a test; CORS and session lifetimes configurable (here as bootstrap configuration, ADR 0011 §6) | ADR 0005; §10 |
| `HttpAuditUserProvider` / `SystemAuditUserProvider` split; AuditTrail gaps to re-check | ADR 0005; §8 |
| Typed settings registry, codecs, bootstrap-only `appsettings`, unknown-key warning | ADR 0011; Admin Settings page |
| Architecture tests with positive-control fixtures (UI never references data packages, no `DateTime` on public surface, dependency deny-list, conversion placement, page gate) | §14; phase 0 |
| Database conventions: snake_case prefixes, `Restrict` FKs, model-convention verifier, index-cites-query | ADR 0004 §12; `docs/DATABASE-CONVENTIONS.md` |
| `TimeProvider` injected, `DateTimeOffset.Now/UtcNow` banned; typed keys; DTOs are records; conventions docs that pair each rule with its enforcement; engineering doc ↔ operator doc twins | `docs/CODING-CONVENTIONS.md` (phase 0); docfx pages |
| Delivery process: gate table, `report-failure.ps1` for scheduled workflows, never `paths:` on a required check; pr-gate / prerelease-review / release / hotfix skills | `docs/DELIVERY-PROCESS.md`, `.claude/skills/` (phase 0) |
| `docs/STACKED-PRS.md` + `scripts/restack.ps1` | copied verbatim |
| `Generate-ThirdPartyNotices.ps1` with `-Check` | phase 0; release skill |
| Regex hygiene (`\A(?:…)\z`, `NonBacktracking`, timeout) | §3 parsers |
| ADR format: "Not chosen" bullets (wms's dated in-place amendments were used until ADR 0000 replaced them); README "Features: on main today / arrives with" table | these ADRs; README at phase 1 |

Deliberately **not** taken (reasons in the review note): global sequence-backed row version, `long`
identity keys, SQL Server + PostgreSQL dual provider, local accounts and HMAC integrity rows,
Data Protection key ring and encrypted connection strings, Native AOT, device sync and manifests,
settings cascade scopes, Kiota generation (ADR 0010), the `Idempotency-Key` header and its record
table (ADR 0012 §4).

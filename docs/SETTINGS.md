# Settings

This document is the contract for Barefoot's settings registry and for what is bootstrap configuration
instead. It restates ADR 0011, with ADR 0001 §1 (the database options), ADR 0005 (who may read and write
settings; CORS and session lifetimes) and ADR 0012 §3 (`If-Match` on a setting). It adds no decision.

Nothing described here is built yet. Each rule names what enforces it and the build phase (DESIGN §13,
phases 0–7) that enforcement arrives with. Points the ADRs leave open are listed in [the gaps note](notes/2026-11-20-convention-gaps.md)
and are marked here as *(open: gap S-n)*.

A **setting** is an operational value an Admin can change without a redeploy. It is defined once in code,
stored in the database, validated, audited, and read through one typed accessor. **Bootstrap configuration**
is what a host needs before or without the database; it lives in `appsettings` and is not a setting.

## The registry

A setting is a typed key: `SettingKey<T>`, declared as a `static readonly` field in `Core`. Each key carries:

| Member | Meaning |
|---|---|
| name | lower-case dotted, for example `run.lease_seconds`; validated at construction |
| default | used when no row is stored |
| description | one line |
| `Validator` | optional; rejects a value the type alone cannot |
| `RequiresRestart` | the Settings page shows a banner until the API restarts. No key in the initial registry sets it: all of them are read per use |

- There are no string overloads anywhere. Code names a key by its field.
- One `SettingRegistry` collects every key at startup and rejects duplicates.
- A key is added **with the code that reads it**, never ahead of it.
- Scope is global. There is no per-site, per-zone or per-user cascade. Per-user preferences (time zone,
  default occupancy) live on `AppUser`, not in the registry.
- A setting's name is part of the API surface. Renaming one is a migration that copies the row.

**Enforced by:** `SettingKey<T>` (name validated at construction) and `SettingRegistry` (duplicates rejected
at startup); `SettingKey<T>` arrives with phase 1, the registry with phase 3; review checklist line: "a new
key ships in the same PR as the code that reads it" (pr-gate skill, phase 0).

## Initial registry

Nine keys. Defaults and validators are exactly those of ADR 0011 §10–§11.

| Key | Type | Default | Validator | Read by |
|---|---|---|---|---|
| `run.lease_seconds` | integer † | 300 | ≥ 60 | the API, on `claim`; returned to the worker as `leaseExpiresUtc` |
| `run.min_lead_days` | integer † | 4 | ≥ 0 | the API, when it resolves check-in windows; never reaches the worker |
| `run.politeness_delay_ms` | integer † | 2500 | ≥ 1000 | handed to the worker in the run's `worker` block |
| `run.politeness_jitter_ms` | integer † | 750 | none stated | handed to the worker in the run's `worker` block |
| `run.max_active_searches_per_user` | integer † | 5 | ≥ 1 | Business; over the limit is `422 search.active_limit_reached` |
| `digest.sender_display_name` | string † | the product name | none stated | digest delivery |
| `digest.max_recipients_per_owner` | integer † | 5 | 0–20 | Business; an owner over the cap is `422` |
| `vocab.stale_after_runs` | integer † | 6 | none stated | the search editor: a vocabulary value not seen in this many runs is greyed |
| `results.flight_options_page_size` | integer † | 10 | 1–50 | the flight-options endpoint's page size |

† The ADR gives each key a name, a default and (for six keys) a floor or range. It does not state the CLR
type; the type shown is read from the default *(open: gap S-1)*.

Validators carry floors where a bad value would hammer the booking site or break a screen.

Deliberately **not** in the registry:

| Key | Why not |
|---|---|
| `digest.retention_days` | no purge job is designed; the key arrives with the purge that reads it |
| `ui.disclaimer_version` | the disclaimer text ships with the deploy, so its version is a constant beside the text |
| `database.free_quota_error_numbers` | bootstrap: read exactly when the database cannot be |
| `auth.session_absolute_hours`, `auth.session_idle_minutes` | bootstrap: the web host applies them before anyone is signed in |
| `auth.cors_origins` | bootstrap: the API applies it at start-up, whether or not the database is awake |

**Enforced by:** the validator runs server-side on every write (Business, phase 3; the endpoint, phase 4);
the Business tests of each floor and range (phase 3).

## Storage

- Values are stored as **invariant text** in one table, `Setting`: `Key` (primary key), `Value`,
  `UpdatedUtc`, `Version`.
- A `SettingCodec<T>` per type converts between the value and the text: bool, int, long, double, string,
  `TimeSpan`, `DateTimeOffset`, string lists.
- **A missing row means the default.**
- The table is audited by `Wolfgang.AuditTrail`. Every change names the Admin who made it.
- **No secret is ever a setting.** `GET /settings` and the audit trail both show values. A credential (for
  example the e-mail provider's) is a Key Vault reference in bootstrap configuration.

**Enforced by:** the codec unit tests (arrive with phase 1 or 3, with the codecs) *(open: gap S-2)*;
review checklist line: "no setting holds a secret" (pr-gate skill, phase 0).

## How code reads a setting

- Read **only** through `ISettings`: `Get(SettingKey<T>)`. It is injected everywhere in `Business` and `Api`.
- No setting is read at type-initialisation time.
- Each API instance keeps a cache. The cache is invalidated by a cheap generation probe: `max(UpdatedUtc)` +
  row count. There is no global row version.
- **The probe runs only on a request**, when the cache is older than 30 seconds. It never runs from a timer:
  a background query would keep the serverless database awake and spend the free allowance.
- A change therefore takes effect **at once** on the instance that wrote it and **within 30 seconds** on
  another instance.
- Tests inject an in-memory `ISettings`.

**Workers do not read the registry.**

- The scraper holds no settings permission. It has no configuration of its own beyond the API address and
  its sign-in.
- `POST /runs` and `POST /runs/resume` return a `worker` block: `politenessDelayMs` and
  `politenessJitterMs`, read from the registry when the call is answered. A resumed run picks up a changed
  value.
- The lease length is the API's alone. `claim` takes no `leaseSeconds`; the API applies `run.lease_seconds`
  and returns `leaseExpiresUtc`.
- `run.min_lead_days` is applied by the API and never sent to the worker.

**Enforced by:** review checklist line: "settings are read through `ISettings`, never from configuration or
a static" (pr-gate skill, phase 0); the Business tests of the cache and its probe, driven by a fake clock
(phase 3); the API contract tests of the `worker` block and of `claim` (phase 4).

## How a setting is changed

| Endpoint | Meaning | Permission |
|---|---|---|
| `GET /settings` | the whole registry | `Permission.SettingsRead` |
| `GET /settings/{key}` | one setting; `ETag` is its `Version` | `Permission.SettingsRead` |
| `PUT /settings/{key}` | set the value; `If-Match` required; the validator runs server-side | `Permission.SettingsWrite` |
| `DELETE /settings/{key}` | remove the row = reset to default; `If-Match` required | `Permission.SettingsWrite` |

- Each entry returned carries: key, type, default, description, current value, `Version`, updated-by and
  updated-at *(open: gap S-3)*.
- Only the **Admin** role holds `SettingsRead` or `SettingsWrite`. No service principal does.
- `If-Match` follows [API-CONVENTIONS.md](API-CONVENTIONS.md): missing or `*` is `428`, stale is `412`.

**The first write and reset.**

| Situation | `Version` / `ETag` | Write |
|---|---|---|
| no row stored (the default applies) | `0` / `"0"` | `PUT` with `If-Match: "0"` inserts the row at `Version` 1 |
| two first writes race | — | they race on the primary key; the loser gets `412` |
| a row is stored | its `Version` | `PUT` with `If-Match: "<Version>"` |
| reset | its `Version` | `DELETE` with `If-Match` removes the row; the row count changes, so the cache probe sees it |

**`bf settings`.** `bf` reads and writes settings through the same endpoints: `bf settings list`, `get`,
`set`, `reset`. It signs the Admin in interactively, so the audit trail names that person. No unattended
identity holds Admin rights.

**Enforced by:** the architecture test that every endpoint declares a permission (scanner in phase 0;
endpoints from phase 4); the API contract tests for `428` / `412`, the `"0"` first write and the reset
(phase 4).

## The Admin Settings page

- The page is called **Settings**. It is Admin-only and is the rail's last item. An ordinary user has no
  Settings page; their own preferences are on Profile.
- It lists the registry: each key with its description, an editable value with inline validation, the
  default, and who last changed it.
- An edited row has **Save**. A changed row has **Reset to default**.
- A key with `RequiresRestart` shows a banner until the API restarts.
- A `412` on Save says the row changed elsewhere and offers Reload.
- Below the registry, **What is running**: the web site's and the API's build and contract revision, the
  schema, and when the scraper was last seen.

**Enforced by:** the page-gate test (every routable page is under the gated layout or carries an
`[Authorize]` policy; scanner in phase 0, pages from phase 7); the per-screen Failed-state test and the
accessibility scan of DESIGN §14 (phase 7).

## Bootstrap configuration (not settings)

`appsettings` holds bootstrap keys only. **Anything a host needs before or without the database is bootstrap
by definition.**

| What | Host | Why it is bootstrap |
|---|---|---|
| the connection string | API, `bf-maint` | needed to reach the database at all |
| the `Database` options (table below) | API, `bf-maint` | they govern connecting and retrying |
| Entra tenant and client ids | API, web host, `bf`, `bf-scraper` | needed to validate or acquire a token |
| hosting flags (`Hosting:BehindProxy`) | API, web host | applied at start-up |
| logging | every host | applied at start-up |
| the CORS allow-list (empty by default) | API | applied at start-up, whether or not the database is awake. The browser never calls the API; the web host does |
| session lifetimes, absolute and idle | web host | applied before any user is signed in; the registry is readable only by a signed-in Admin |
| secrets (the e-mail sender's credential) | API | a Key Vault reference; no secret is a setting |
| the API address and the sign-in | `bf-scraper`, `bf` | the scraper has no other configuration. Service-principal secrets live in user-secrets on the home machine, never in the repository |

The `Database` options section (ADR 0001 §1):

| Option | Default | Why |
|---|---|---|
| `ConnectTimeoutSeconds` | 90 | resume can take 60 s; Microsoft says ≥ 60 |
| `CommandTimeoutSeconds` | 60 | the free tier is slow on first touch after resume |
| `MaxRetryCount` | 8 | with a 20 s cap ≈ 2 min of transient-error retries |
| `MaxRetryDelaySeconds` | 20 | |
| `AdditionalTransientErrorNumbers` | `[4060]` | "cannot open database" appears on some resume paths |
| `FreeQuotaErrorNumbers` | `[42119]` | community-reported, unverified; a wrong guess costs one configuration change |
| `FreeQuotaMessagePattern` | `monthly free allowance\|free limit` | second signal for the quota state |
| `WarmUpGraceSeconds` | 2 | how long a UI waits before showing "Warming up" |

`FreeQuotaErrorNumbers` and `FreeQuotaMessagePattern` stay here because they are read exactly when the
database cannot be. The SQLite test path never sees these options.

Key names other than `Hosting:BehindProxy` and the `Database` options are not fixed by an ADR
*(open: gap S-4)*.

### Unknown keys warn at start-up

An unknown key under the application's configuration section produces a start-up warning. A value typed
into `appsettings` by mistake cannot hide there: it is either a known bootstrap key or it is reported.

**Enforced by:** the start-up check that logs the warning, with a test that an unknown key is reported
(arrives with phase 4, with the API host) *(open: gap S-5)*.

## Adding a setting

1. Confirm it is not bootstrap: can the host read it from the database at the moment it needs it? If not, it
   is bootstrap configuration.
2. Confirm it is not a secret.
3. Declare the `SettingKey<T>` in `Core`: lower-case dotted name, default, one-line description.
4. Add a `Validator` if a bad value would hammer the booking site or break a screen.
5. Read it through `ISettings.Get(key)` at the point of use, in the same PR.
6. If a worker needs the value, the API hands it over in a response; the worker does not read the registry.
7. Add the key to the table in this document.

# ADR 0011 — Settings: a typed registry stored in the database; `appsettings` holds bootstrap keys only

- **Status:** Accepted, 2026-11-20 (proposed 2026-11-06; amended on acceptance: the first write and
  reset of a setting, how workers receive their values, the cache probe, validator floors, no
  secrets, and three keys removed from the initial registry)
- **Scope:** `Core` (`SettingKey<T>`), `Business` (`ISettings`, registry), `Data` (`Setting` table),
  `Api` (`/settings`, the run resource), `Web` (Admin → System), `Admin` (`bf settings`),
  `appsettings.*`

## Context

Barefoot has a growing set of operational knobs — lease length, politeness delay, minimum lead
days, max active searches per user, digest retention, run-order defaults, disclaimer version,
session lifetimes, CORS allow-list, quota error numbers — that an Admin should be able to change
without a redeploy, that must be validated, audited and visible, and that every host must read the
same way. Scattering them across `appsettings.json`, environment variables and container secrets
makes "what is the effective value right now" a question nobody can answer from the UI.

`Chris-Wolfgang/wms` solved this with a typed setting registry (its `docs/SETTINGS.md`): the shape
below is that design with the multi-site cascade removed.

## Decision

1. **A setting is a typed key**: `SettingKey<T>` declared as a `static readonly` field in `Core`
   with a lower-case dotted name (`run.lease_seconds`), a default, a one-line description, an
   optional `Validator`, and `RequiresRestart`. Names are validated at construction; there are no
   string overloads anywhere.
2. **One `SettingRegistry`** collects every key at startup and rejects duplicates; `GET /settings`
   and `GET /settings/{key}` return the registry (key, type, default, description, current value,
   `Version`, updated-by/at) for the Admin **System** page. Both need `Permission.SettingsRead` and
   writes need `Permission.SettingsWrite`; only the Admin role and the `Admin` host's service principal (`bf`,
   ADR 0005) hold either.
3. **Values are stored as invariant text** in one `Setting` table (`Key` PK, `Value`, `UpdatedUtc`,
   `Version`) through a `SettingCodec<T>` per type (bool, int, long, double, string, `TimeSpan`,
   `DateTimeOffset`, string lists). Missing row = default. The table is audited by
   `Wolfgang.AuditTrail`, so every change names the Admin who made it.
4. **Read only through `ISettings`** (`Get(SettingKey<T>)`), injected everywhere in `Business` and
   `Api`. A per-instance cache is invalidated by a cheap generation probe (`max(UpdatedUtc)` +
   count — no global row version, ADR 0004). **The probe runs only on a request**, when the cache is
   older than 30 seconds, and never from a timer: a background query would keep the serverless
   database awake and spend the free allowance (ADR 0001). A change therefore takes effect within
   30 seconds on another instance and at once on the instance that wrote it.
5. **Write through `PUT /settings/{key}`** with `If-Match` on the setting's `Version` (ADR 0012);
   the validator runs server-side.
   - A setting with no row reports `Version` 0 and `ETag: "0"`; its first write is
     `If-Match: "0"` and inserts the row at `Version` 1. Two first writes race on the primary key and
     the loser gets `412`.
   - `DELETE /settings/{key}` with `If-Match` removes the row, which is "reset to default". The
     row count changes, so the probe sees it.
   - `RequiresRestart` stays on `SettingKey<T>` and shows a banner on the System page until the API
     restarts. No key in the initial registry sets it: all of them are read per use.
6. **`appsettings` holds bootstrap keys only**: the connection string and every database option of
   ADR 0001 §1, Entra tenant/client ids, the hosting flags (`BehindProxy`), logging. Anything the
   API needs **before or without the database** is bootstrap by definition — which is why
   `FreeQuotaErrorNumbers` and `FreeQuotaMessagePattern` stay there: they are read exactly when the
   database cannot be. An unknown key under the application's section produces a startup warning so
   a setting cannot hide there by mistake.
7. **Scope is global.** No per-site, per-zone or per-user cascade; per-user preferences (time zone,
   default occupancy) live on `AppUser`, not in the registry.
8. **No secret is ever a setting.** `GET /settings` and the audit trail both show values. A key
   such as an e-mail provider credential is a Key Vault reference in bootstrap configuration.
9. **Workers do not read the registry.** The scraper holds no settings permission. The run resource
   returned by `POST /runs` and `POST /runs/resume` carries a `worker` block — `politenessDelayMs`
   and `politenessJitterMs` — read from the registry when the call is answered, so a resumed run
   picks up a changed value. `run.min_lead_days` never reaches the worker: the API applies it when
   it resolves check-in windows (DESIGN §4). The lease length is the API's alone: `claim` takes no
   `leaseSeconds`, the API applies `run.lease_seconds` and returns `leaseExpiresUtc`. `bf` reads and
   writes settings through the same endpoints as its service principal (`bf settings list`, `get`,
   `set`, `reset`); the audit trail names that principal (ADR 0005).
10. **Validators carry floors where a bad value locks people out or hammers the site**:
    `auth.session_idle_minutes` ≥ 5, `auth.session_absolute_hours` ≥ 1 and not shorter than the idle
    time, `run.lease_seconds` ≥ 60, `run.politeness_delay_ms` ≥ 1000, `run.min_lead_days` ≥ 0,
    `run.max_active_searches_per_user` ≥ 1, `results.flight_options_page_size` 1–50. Each
    `auth.cors_origins` entry is an absolute `https` origin with no path. `bf settings set`
    authenticates with client credentials, not a web session, so it still works when a session
    setting is wrong.
11. **Initial registry** (defaults in parentheses): `run.lease_seconds` (300), `run.min_lead_days`
    (4), `run.politeness_delay_ms` (2500) and `run.politeness_jitter_ms` (750),
    `run.max_active_searches_per_user` (5), `digest.sender_display_name` (the product name),
    `vocab.stale_after_runs` (6), `auth.session_absolute_hours` (8), `auth.session_idle_minutes`
    (30), `auth.cors_origins` (empty), `results.flight_options_page_size` (10).

    A key is added with the code that reads it, never ahead of it. Left out on that rule:
    - *`digest.retention_days`* — no purge job is designed (ADR 0006); it arrives with the purge.
    - *`ui.disclaimer_version`* — the disclaimer text ships with the deploy, so its version is a
      constant beside the text (ADR 0008). As a setting it could be raised without a text change,
      or left alone after one.
    - *`database.free_quota_error_numbers`* — bootstrap, item 6.

## Not chosen

- *`IOptions<T>` from `appsettings` / environment only.* Every change is a redeploy; no audit; no
  UI; the effective value is invisible.
- *A settings cascade (org → site → zone).* Right for a multi-tenant warehouse product; barefoot is
  one installation.
- *Azure App Configuration.* Another service, another identity, for a dozen keys.

## Consequences

- The Admin **System** page (DESIGN §12) is read/write rather than read-only, with a reset action
  per setting.
- DESIGN §5: `claim` loses `leaseSeconds` and returns `leaseExpiresUtc`; the run resource gains the
  `worker` block. The scraper has no configuration of its own beyond the API address and sign-in.
- Tests inject an in-memory `ISettings`; no setting is read at type-initialisation time.
- A setting's name is part of the API surface; renaming one is a migration that copies the row.

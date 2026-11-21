# ADR 0005 — Identity: Entra ID, invite-only guests, app roles, no stored passwords

- **Status:** Accepted, 2026-11-06 (amended 2026-11-06: endpoint rule, revocation, web token flow;
  2026-11-20: the `Scraper` role's reach, a `DigestSender` role, `bf` by hand signs the user in,
  who sets *sessions valid after*, CORS and session lifetimes are bootstrap configuration)
- **Scope:** `Api` authentication/authorization, `Web` sign-in, `Scraper`/`Admin` credentials,
  database identities, audit

## Context

The API is a public endpoint (ADR 0002) serving one Admin and up to a dozen unrelated users who
sign in with whatever accounts they already have. The audit trail must name the person. The
scraper and admin tools run unattended and need a non-interactive identity. The database must be
reachable by the API without a password anywhere.

## Decision

- **Entra ID in the existing tenant** (the Azure subscription's directory). One app registration
  with *assignment required*, so only assigned users can sign in. Users are invited as **B2B
  guests** and sign in with a Microsoft account, a federated Google account, or a one-time passcode
  to any e-mail. No sign-up page; no password the application stores or resets.
- **App roles**, assigned per user in Entra and read as claims: `Admin` (everything), `Editor`
  (create/edit/deactivate own searches; see own results and own searches' run outcomes), `Viewer`
  (read own), and two roles held only by service principals:
  - **`Scraper`** (`bf-scraper`): *execute runs* — start, resume, `claim`, `result`, `complete`,
    `abort`, `finish` — and *write catalog snapshots*. Starting a run explodes **every** user's
    active searches, so the role reaches all of them as work; it sits between an Editor (own
    searches only) and Admin (everything). It cannot list or read searches, results, digests or
    users: the API hands it lookups, never a search.
  - **`DigestSender`** (`bf send-digests` in the scheduled job): *send pending digests* and *read
    delivery status*. Nothing else.

  Runs, reference data, deliveries and failure details are otherwise Admin-only.
- **The API validates Entra JWTs itself** (`Microsoft.Identity.Web`): one gate for UI, scraper and
  admin. The API **never redirects**; `401 auth.not_signed_in` and `403 auth.forbidden` are problem
  details (ADR 0012).
- **Every endpoint declares its permission** — `RequirePermission(Permission.X)` or
  `AllowAnonymous()` with a comment naming why — and an architecture test fails an endpoint that
  does neither. Permissions are typed keys (`Permission`), mapped to roles in one table.
- **Revocation before token expiry.** Roles in a JWT are stale until it expires, so the API reads
  the caller's `AppUser` row per request (short cache) and honours a per-user *sessions valid after*
  instant (`AppUser.SessionsValidAfterUtc`): a token issued before it is `401`. Roles are assigned
  in Entra, where the API cannot see a change, so the instant is set by the Admin — a **Sign out
  everywhere** action on the Users page, used after changing or removing someone's role.
- **The web host signs users in with OIDC** and keeps a server-side session cookie; it acquires the
  API token for the user server-side and calls the API with it. The token never reaches browser
  script; the API sees the person, not the web host.
- **Identity key = `oid`** (object id), a GUID stable for the life of the user object in the
  directory; parsed once at the auth boundary into a `Guid`. Never `email`/`preferred_username`
  (changeable) or `sub` (per application).
- **Audit:** `HttpContextAuditUserProvider` records the user's `oid` and display name, or the
  service principal's name for the scraper's and the digest sender's writes;
  `SystemAuditUserProvider` names the job for unattended work. `user_id` is the host application,
  `on_behalf_of` is the person.
- **Unattended clients** use client-credentials service principals, one per role: `bf-scraper` as
  `Scraper`, and `bf` in the scheduled job as `DigestSender`. Secrets live in user-secrets on the
  home machine, never in the repository. **`bf` run by hand signs the user in interactively** and
  acts with that person's own roles — `bf settings` and `bf search list` need an Admin — so the
  audit trail names a person, and no unattended identity holds Admin rights.
- **Database identities, no passwords:** `Authentication=Active Directory Default`; the API's
  managed identity gets `db_datareader` + `db_datawriter`; DDL rights belong to the operator's Entra
  user (local) or the pipeline job's managed identity. `bf-maint grant-roles` creates the
  contained users and roles idempotently and runs as part of `migrate`.
- **Hardening:** the web host sends CSP (`default-src 'self'`, `frame-ancestors 'none'`),
  `X-Content-Type-Options: nosniff`, a referrer policy and a permissions policy, checked by an
  integration test; the API's CORS allow-list (empty by default — the browser never calls the API,
  the web host does) and the web host's session lifetimes (absolute and idle) are bootstrap
  configuration, not registry settings (ADR 0011 §6); authentication endpoints are never
  response-compressed (BREACH); forwarded
  headers are honoured only when `Hosting:BehindProxy` is set.

## Not chosen

- *ASP.NET Core Identity / local accounts.* The application would own passwords, resets and MFA for
  a dozen non-technical people, plus a break-glass admin and its storage.
- *Entra External ID (customer tenant).* Built for self-service sign-up at scale; more tenant than
  an invited dozen needs. Revisit if sign-up ever becomes a requirement.
- *Container Apps built-in auth as the only gate.* Fine for the web host; the API still has to
  validate tokens for the unattended clients, so it is the gate.
- *Trusting roles from the token alone.* Cheapest, but a disabled user keeps working until the token
  expires.

## Consequences

- No credential in the repository or the database; secrets are Entra app secrets and user-secrets.
- Adding a user = invite + assign role; removing = unassign (their rows stay, owned by their `oid`).
- `AppUser` (ADR 0004) is upserted at sign-in so lists can show names.
- Users cost nothing; searches do (politeness delay × lookups, DB awake time). A
  `run.max_active_searches_per_user` setting (ADR 0011) is enforced in the BLL from day one.

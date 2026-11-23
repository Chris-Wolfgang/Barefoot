# Architecture Decision Records

This folder records the design decisions behind this repository that the code cannot explain on its own. See
[ADR 0000](0000-record-architecture-decisions.md) for the practice, and copy [`TEMPLATE.md`](TEMPLATE.md) to add a
record.

A record is not rewritten once **Accepted**. A reversed decision gets a new record that supersedes the old one, and
the old one stays with its status updated. A partly changed decision gets a new record that amends the old one, which
stays Accepted and gains `Amended by [NNNN]` in its status. If the decision is unchanged, typos and broken links are
fixed directly and a factual correction is a dated note beside the original text. Numbers never change and are never
reused; a retired number keeps its row below. The dated amendments in some records predate this rule and stay as they
are.

| # | Title | Status |
|---|-------|--------|
| [0000](0000-record-architecture-decisions.md) | Record architecture decisions | Accepted |
| [0001](0001-azure-sql-serverless-database.md) | The database is Azure SQL Database (free offer, serverless) and the app tolerates it sleeping | Accepted |
| [0002](0002-everything-through-a-web-api.md) | Every client reaches the database through one Web API; DAL and BLL are separate layers | Accepted |
| [0003](0003-api-owns-the-run-lifecycle.md) | The API owns the run lifecycle; the scraper pulls work | Accepted |
| [0004](0004-shared-requests-and-matrix-ownership.md) | Shared search requests, owner on the search, visibility by join | Accepted |
| [0005](0005-identity-entra-invite-only.md) | Identity: Entra ID, invite-only guests, app roles, no stored passwords | Accepted |
| [0006](0006-email-stored-digests-separate-send.md) | E-mail: store the digest on completion, send in a separate step, owner-only recipients | Accepted |
| [0007](0007-three-hosts-scraper-admin-maintenance.md) | Three small hosts: scraper, admin, maintenance | Accepted |
| [0008](0008-web-ui-fluent-components-own-state.md) | Web UI: Blazor Web App on Fluent UI Blazor; components own their state; role-scoped IA | Accepted |
| [0009](0009-naming-wolfgang-barefoot.md) | Naming: `Wolfgang.Barefoot` is the code name; the product name is a display string | Accepted |
| [0010](0010-api-client-hand-written-over-a-frozen-openapi-contract.md) | API client: hand-written over a committed, frozen OpenAPI contract | Accepted |
| [0011](0011-settings-registry.md) | Settings: a typed registry stored in the database; `appsettings` holds bootstrap keys only | Accepted |
| [0012](0012-api-conventions-errors-concurrency-idempotency-paging.md) | API conventions: typed error codes in problem details, ETag concurrency, idempotency by state, keyset paging | Accepted |
| [0013](0013-documentation-site-docfx-for-operator-pages.md) | Documentation site: keep DocFX and Pages, for operator pages | Proposed |

## Status legend

- **Accepted**: in force.
- **Superseded by NNNN**: replaced by a later record; kept for history.
- **Deprecated**: no longer applies, and nothing replaced it.
- **Retired**: the number was used and the record removed; the number is not reused.

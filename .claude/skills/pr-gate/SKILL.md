---
name: pr-gate
description: Author-side checklist for every Barefoot pull request. Covers scope and base, the acceptance-criteria-to-test map, layer rules, docs, the changelog fragment, the data model, the API contract, exit codes, parsers and new dependencies. Run it before opening a pull request and again after any push that changes its scope.
---

# PR gate

Run this before `gh pr create`, and again after a push that changes what the pull request does.
CI checks what it can. This list covers what only the author knows.
See [docs/DELIVERY-PROCESS.md](../../../docs/DELIVERY-PROCESS.md) for the gates and what enforces
each rule.

Sections 6 to 9 apply only once their phase has started (DESIGN §13). If a section does not apply,
write "not applicable" under its heading in the pull request body. Do not delete the heading.

## 1. Scope, base and title

- One concern per pull request. If it grew, split it.
- The base is `develop`, or the pull request below this one in the stack. Never `main`, except for
  the release pull request and a configuration-only pull request that must reach `main` first.
- Before branching: `git fetch --prune origin`, then branch from `origin/develop` or from the branch
  below. Before opening: `git log --oneline origin/<base>..HEAD` lists only this pull request's
  commits.
- Title: `type(scope): summary` in the imperative, for example `feat(core): resolve recurring
  check-in windows`. Types: `feat`, `fix`, `docs`, `test`, `refactor`, `ci`, `build`. The scope is
  the layer or area.
- Label the pull request with its phase (`phase-0` … `phase-7`).
- **Protected files go alone.** Workflows, `.editorconfig`, `*.globalconfig`, `*.DotSettings`,
  `Directory.Build.props`, `BannedSymbols.txt`, `coverlet.runsettings`,
  `.config/dotnet-tools.json`, `.github/license-audit/*.json` and the CI scripts are changed in a
  `config/<short-name>` pull request with nothing else in it. `Protected Files Guard` fails a mixed
  pull request and nothing waives it.

## 2. Every acceptance criterion maps to a test

In the pull request body, a table: criterion → test name. Use "doc" for a criterion that is
documentation only. If a later phase delivers a criterion, name the phase and say why.

A criterion without a test is untested or wrongly scoped. Say which.

A new architecture rule comes with a **positive control**: a fixture the rule must flag, asserted
next to the real assertion. A scanner that cannot fail proves nothing.

Tests use real classes. The test provider for data is SQLite. No test reaches the booking site,
Azure or Entra.

## 3. Layer rules

The project a change lands in decides what it may reference (DESIGN §2). The architecture tests
fail a violation; check before you push.

| Project | Must not reference |
|---|---|
| `Core`, `Presentation` | any framework |
| `Scraper`, `Admin`, `Web` | `Data`, any EF Core package |
| `Admin` | `Obe` |
| `Maintenance` | `ApiClient`, `Obe` |
| `Api` | `Obe` |

- Every client reaches the API through `ApiClient` only. `Maintenance` is the one project that
  opens the database directly.
- Registration is explicit. No assembly scanning.
- Time comes from an injected `TimeProvider`. `DateTimeOffset.Now` and `UtcNow` are banned
  (DESIGN §8). No `DateTime` on a public surface.
- A new project reference that this table does not cover needs a line in DESIGN §2 in the same
  pull request.

## 4. Docs in the same pull request

- Public members have XML docs. `CS1591` fails the Release build.
- A changed rule changes its page: `docs/CODING-CONVENTIONS.md`, `API-CONVENTIONS.md`,
  `DATABASE-CONVENTIONS.md`, `MIGRATE.md` or `SETTINGS.md`. Each rule on those pages names what
  enforces it.
- A decision with alternatives is an ADR under `docs/adr/`. An accepted ADR is not
  rewritten: a changed decision is a new ADR that supersedes it (ADR 0000).
- A change to behaviour that DESIGN.md describes changes DESIGN.md.

## 5. Changelog fragment

A change under `src/` needs `changelog/unreleased/<short-change-name>.md`. Line 1 is
`type: breaking|feature|fix|docs|internal|dependencies`; then one sentence a user can read.

- No user-visible effect: the `no-changelog` label and a reason in the body. Add the label before
  the push that should pass; the check reads the labels of the event that started it.
- `breaking` means a break in the HTTP contract. Say which endpoint or record changed.

## 6. Data model (from phase 2)

- Until the first release there is one migration, `InitialCreate`. A model change regenerates it.
  After the first release every change is a new migration with a working `Down`; CI applies up,
  down, up on SQL Server.
- No raw SQL and no provider-specific column type outside the migration. The same model runs on
  Azure SQL and on SQLite in tests.
- A row a user edits or a worker claims has a `Version` column. Every foreign key is `Restrict`.
  Names are snake_case with the `pk_/ak_/fk_/ix_/ux_` prefixes. The model-convention verifier test
  fails otherwise.
- Every new index names the query it serves, in a comment.
- State the review in the body: what the change does to existing rows, and whether `Down` loses
  data. No model change: write "no model change".

## 7. API contract (from phase 4)

- The committed OpenAPI document equals the served one. Regenerate `docs/api/openapi-v{n}.json`
  and commit it; a test fails on a difference.
- A breaking change to `v0` raises the **contract revision** and carries a `breaking` fragment.
  The `openapi-diff` job fails a breaking diff with an unchanged revision. A version listed in
  `docs/api/frozen.txt` does not change.
- Every endpoint declares a permission, or `AllowAnonymous()` with a comment that says why.
- Errors are problem details with a typed `ErrorCode`. A write is safe to repeat because of its own
  state. An edit needs `If-Match`: `428` without it, `412` when stale (ADR 0012).
- A new `ApiClient` method has a contract test through `WebApplicationFactory` and SQLite.

## 8. Command-line hosts (from phase 2 for `bf-maint`, phase 5 for `bf-scraper` and `bf`)

- A new way to end uses the shared `ExitCode` enum in `Core`. No host invents a number. A new
  member regenerates `docs/EXIT-CODES.md`; a test fails when the committed file differs.
- Check the semiweekly job's gate: it stops on 5, 7, 8 and 9 and continues past 2 and 3. A new
  code needs a decision on which side it falls, written in ADR 0007.
- stdout carries only the verb's output, culture-invariant. Logs go to stderr and the rolling
  file. No `Console.Write` for logging.
- Every `bf-scraper` and `bf` verb except `version` checks `GET /version` first.

## 9. Booking-site adapters (from phase 5)

- A parser change comes with a fixture: captured markup under the test project and a test that
  pins the parsed values. Redact anything personal from the capture.
- Regular expressions are anchored (`\A(?:…)\z`), `NonBacktracking`, and have a timeout.
- The scraper reads no settings of its own. Delay, jitter and lease values come from the API with
  the run.
- Nothing in a pull request's tests talks to the booking site. The live check is the scraper soak
  in `prerelease-review`.

## 10. New dependency

A new `PackageReference` outside the BCL and Microsoft packages needs, in the body:

- why the BCL does not cover it;
- its license, which must be in `.github/license-audit/allowed-licenses.json` (`License Audit`
  fails otherwise; changing that file is a configuration-only pull request);
- the interface it sits behind.

Then run `pwsh ./scripts/Generate-ThirdPartyNotices.ps1` and commit `THIRD-PARTY-NOTICES.md`.

## 11. Local gate before push

- `pwsh ./scripts/build-pr.ps1 -CoverageThreshold 95` passes: Release build with zero warnings, all
  tests, 95 % line coverage on each `src/` assembly, 100 % on each test assembly, DevSkim,
  gitleaks. Paste its summary line into the body.
- If the test project has a `stryker-config.json`, run `dotnet stryker` in it and confirm the score
  is at or above `thresholds.break`.
- `build-pr.ps1` does not run InspectCode or the changelog check. Read those two on the pull
  request.

## 12. Pull request body

```
Refs #<issue>.            (or "Closes #<issue>" when this pull request finishes it)
Stacked on #<pr>. Merge that first, then restack.     (only when stacked)

## What
## Criteria → tests
| Criterion | Test |
## Docs
## Data model
## API contract
## Hosts and exit codes
## Dependencies
## Verification
```

Claim only what was run. Under "Verification", name the command and paste its last line.

`Closes #N` closes the issue only when the commit reaches `main`. A merge into `develop` leaves the
issue open until the release.

## After opening

Wait for every check on the latest commit. A check that returned nothing is unknown, not green.
Answer each review thread with what changed and the commit that changed it, then resolve it.

# Delivery process

How a change travels from a pull request to a release, and what enforces each rule on the way.

This page is the map. The checklists a developer or a session runs at each gate are in
[`.claude/skills/`](../.claude/skills/). The design is in [DESIGN.md](DESIGN.md): the phases in §13,
the quality bar in §14.

Each rule below names what enforces it. Where nothing automatic enforces a rule yet, the page says
so and names the phase in which the enforcement arrives. "Arrives with phase N" means it does not
exist today.

## Gates

| Gate | Trigger | What runs | Workflow or skill |
|---|---|---|---|
| **PR** | Every pull request into `develop` or `main` | Secrets scan, project detection, changelog fragment check, InspectCode, tests and coverage gate on Linux and Windows, DevSkim, CodeQL, Semgrep, license audit, SBOM, SourceLink, workflow lint, the protected-files guard, Stryker when test or source files changed | `pr.yaml`, `protected-files.yaml`, `codeql.yaml`, `semgrep.yaml`, `license-audit.yaml`, `sbom.yaml`, `sourcelink.yaml`, `actions-audit.yaml`, `stryker.yaml`; skill `pr-gate` |
| **develop** | A pull request is squash-merged into `develop` | Nothing new. No workflow runs on a push to `develop`; the gate is the PR gate of the pull request that merged | none |
| **main** | A push to `main` (the release pull request, or a Dependabot pull request, merged) | The PR checks again on the merged result, CodeQL, Semgrep, license audit, SBOM, SourceLink, workflow lint, Scorecard; benchmarks when `src/` or `benchmarks/` changed | `pr.yaml`, `codeql.yaml`, `semgrep.yaml`, `license-audit.yaml`, `sbom.yaml`, `sourcelink.yaml`, `actions-audit.yaml`, `scorecard.yaml`, `benchmarks.yaml` |
| **prerelease** | `develop` is a release candidate, before the release pull request is opened | A read of every change since the last release; the scraper soak from phase 5 | skill `prerelease-review` |
| **release** | A GitHub release is published from tag `vX.Y.Z` on `main` | Tag and version check, Release build, all tests, coverage gate, documentation build and deploy | `release.yaml`; skill `release` |

There is no hotfix gate. Release branches and a support window are decided at 1.0 (DESIGN §15);
until then a fix is an ordinary pull request and the next release carries it.

## Branches

- **`main` is the release branch.** Every commit on it is a release, a Dependabot update or a
  configuration change. *Enforced by* the ruleset "Protect main branch": pull request required,
  squash or rebase merge only, linear history, no force-push, no deletion, and these required
  checks: `Protected Files Guard`, `Detect .NET Projects`, `Secrets Scan (gitleaks)`,
  `Changelog Fragment Check`, `Stage 1: Linux Tests + Coverage Gate`, `Stage 2: Windows Tests`,
  `Security Scan (DevSkim)`, `Security Scan (CodeQL) (csharp)`. (Read from the ruleset on
  2026-11-20.)
- **Work lands on `develop`.** Every change is a small pull request into `develop`, or into the
  pull request below it in a stack. The same workflows run for a pull request into `develop` as
  for one into `main`. *Enforced by* the `branches:` lists in `pr.yaml`, `protected-files.yaml`,
  `codeql.yaml` and `stryker.yaml`. **`develop` has no ruleset today**, so no check is *required*
  there: the rule "merge only when every check is green" is enforced by the `pr-gate` skill and by
  the person who merges.
- **Stacked pull requests.** A phase is one stack of small pull requests on `develop`, each
  reviewable alone. Each branch starts from the branch below it and its pull request targets that
  branch. After the bottom pull request is squash-merged, the rest are restacked with
  `scripts/restack.ps1`. The whole procedure is in [STACKED-PRS.md](STACKED-PRS.md); read
  `develop` wherever that page says `main`. *Enforced by* `restack.ps1`, which replays only each
  pull request's own commits and checks each pull request's base.
- **One concern per pull request.** A pull request that grows is split. *Enforced by* review and
  the `pr-gate` skill.
- **`main` flows down into `develop`.** Dependabot opens its pull requests against `main`
  (`.github/dependabot.yml` sets no target branch). After they merge, `main` is merged into
  `develop`. That merge needs no pull request. Nothing enforces how often it happens; the
  `release` skill does it before the release pull request so the release is not behind `main`.
- **Release configuration only.** Every workflow builds and tests the Release configuration.
  *Enforced by* the build steps in `pr.yaml` and `release.yaml`.

## Phases

DESIGN §13 splits the build into eight phases. Each phase is one stack on `develop`. Application
code starts only on an explicit go.

| Phase | Content | What it adds to this process |
|---|---|---|
| 0 | Conventions and gates | This page, the skills, `scripts/report-failure.ps1`, `scripts/Generate-ThirdPartyNotices.ps1`, the architecture test project |
| 1 | Core + Contracts | The first `src/` and `tests/` projects: the test stages, the coverage gate and Stryker start to do real work |
| 2 | Data + Maintenance | The migration check (up, down, up on SQL Server) in CI; the migration review in `pr-gate` |
| 3 | Business | No new gate |
| 4 | Api + ApiClient | The committed OpenAPI document, the `frozen.txt` diff gate, the contract revision, the contract tests |
| 5 | Obe + Scraper + Admin | Parser fixture tests; the scraper soak in `prerelease-review` |
| 6 | Azure | Deployment, the semiweekly scheduled task and its failure reporting |
| 7 | Web | The axe accessibility scan over every routable page |

Until phase 1 merges, `src/` is empty and `tests/` holds the test kit and the architecture tests.
The test stages, InspectCode and Stryker run on those; the license audit and the SBOM skip their
work because they read `src/`. Every check still reports, so pull requests are not blocked.

## The PR gate

`pr.yaml` runs on every pull request into `develop` or `main`. It runs the pull request's own copy
of the code and of the workflow.

| Rule (DESIGN §14) | Enforced by |
|---|---|
| No secret in the diff | `pr.yaml` job `Secrets Scan (gitleaks)` |
| The solution builds with warnings as errors | `TreatWarningsAsErrors` for Release in `Directory.Build.props`; every CI build is Release |
| Tests pass on Linux and Windows; a framework on which zero tests ran fails | `pr.yaml` Stage 1 and Stage 2. Stage 3 (macOS) runs only when the repository variable `PR_PLATFORMS` names it; here it is `linux,windows` |
| 95 % line coverage on every `src/` assembly | `pr.yaml` coverage gate in each stage, per assembly (`CODECOV_MINIMUM: 95`, also in `release.yaml`) |
| 100 % line coverage on every test assembly | `pr.yaml` coverage gate (`CODECOV_TEST_MINIMUM: 100`) |
| Static analysis: CodeQL, DevSkim, Semgrep, InspectCode | `codeql.yaml` (C# and workflow files), `pr.yaml` job `Security Scan (DevSkim)`, `semgrep.yaml`, `pr.yaml` job `ReSharper InspectCode` (an error-severity finding fails the job; warnings go to code scanning) |
| Mutation testing per test project with a break threshold | `stryker.yaml`: it runs every `tests/**/stryker-config.json`, and `dotnet stryker` fails below that file's `thresholds.break`. `MutationFloorTests` fails a `.Tests.Unit` project that has no such file or sets no floor, so the run cannot be skipped by leaving the file out |
| A changelog fragment on every pull request that changes `src/` | `pr.yaml` job `Changelog Fragment Check` (`scripts/changelog.ps1 check`) |
| Every dependency has an allowed license | `license-audit.yaml` against `.github/license-audit/*.json`, read from `main` |
| Architecture rules (project references, no `DateTime` on a public surface, dependency deny-list, every endpoint declares a permission, every page is gated) | `tests/Wolfgang.Barefoot.Tests.Architecture`, each rule with a fixture it must reject. They run inside the test stages. The endpoint and page rules arrive with phases 4 and 7 |
| Copilot review and the owner's review on every pull request | The person who merges. The ruleset requires zero approvals, so nothing automatic enforces this |
| Workflow files are valid and pinned | `actions-audit.yaml` (actionlint, zizmor) |

The author's side of the gate is the `pr-gate` skill: what only the author knows, such as the test
that proves each acceptance criterion and the reason for a new dependency.

`scripts/build-pr.ps1` runs the Windows stage locally: build, tests, coverage, DevSkim, gitleaks. It
does not run InspectCode or the changelog check, so a clean local run does not prove a green pull
request.

### A required check never uses a `paths:` filter

**Rule.** A workflow whose job is a required status check has no `paths:` or `paths-ignore:` filter
on its trigger.

**Reason.** When the filter excludes a pull request, the workflow does not start. A check that
never starts never reports. The ruleset keeps waiting for it, the pull request shows "Expected"
with no end, and nothing on the page says why.

**What to do instead.** Leave the trigger unfiltered and decide inside the job. `pr.yaml` does
this: `Detect .NET Projects` always runs and the later jobs skip their work when there is nothing
to test. Or keep the filter and do not make the check required. `stryker.yaml` does that: it is
filtered to `src/**` and test code, and its header says never to require it.

**Enforced by** review of the configuration-only pull request that changes a workflow trigger, and
by whoever edits the ruleset. No automatic check exists.

The same symptom has a second cause: GitHub disables scheduled workflows in a repository with no
commits for 60 days, and that also stops the workflow's pull-request runs. Repository baseline
item 24 covers it.

## Protected files

Some files decide what CI checks. A pull request must not change them together with the code they
check.

**Rule.** A pull request changes protected files only, or no protected file. A pull request that
only changes protected files is a *configuration-only* pull request and is reviewed as one.

**Protected:** `.github/workflows/*`, `.github/license-audit/*.json`, `.github/requirements/*`,
`.editorconfig` (any directory), `*.globalconfig`, `*.ruleset`, `*.DotSettings`,
`Directory.Build.props` and `Directory.Build.targets` (any directory), `BannedSymbols.txt`,
`coverlet.runsettings`, `.config/dotnet-tools.json`, `.gitleaks.toml`, `scripts/changelog.ps1`,
`scripts/tfm-parity.ps1`, `scripts/build-pr.ps1`, `scripts/third-party-notices.ps1`.

**Enforced by** `protected-files.yaml`, job `Protected Files Guard`. It runs from `main`'s copy of
the workflow, never checks out the pull request, and lists the changed files through the API. A
mixed pull request fails. Nothing can waive the failure; the pull request is split. Dependabot
pull requests are exempt.

Consequences:

- A change to an analyzer rule, a coverage threshold or a workflow is its own pull request, named
  `config/<short-name>`, merged before the code that depends on it.
- The guard also runs on the release pull request from `develop` into `main`. A protected-file
  change that reached `develop` in a stack therefore goes to `main` first, in its own
  configuration-only pull request, and `main` is then merged into `develop`. Otherwise the release
  pull request mixes protected files with code and fails. The `release` skill checks this before
  it opens the pull request.
- The guard itself changes only when its new version reaches `main`.
- `license-audit.yaml` reads the license policy from `main`, so a pull request cannot allow a
  license for itself.

## Changelog fragments

`CHANGELOG.md` is never edited by hand.

- **Rule.** A pull request that changes `src/` adds one file,
  `changelog/unreleased/<short-change-name>.md`. Line 1 is `type: <kind>`; the rest is one sentence
  a user can read. Kinds: `breaking`, `feature`, `fix`, `docs`, `internal`, `dependencies`.
  *Enforced by* `Changelog Fragment Check`.
- **No user-visible effect.** The `no-changelog` label waives the fragment. The check reads labels
  from the event that started it, so add the label and then push. Dependabot pull requests are
  exempt.
- **`breaking` in Barefoot** means a break in the HTTP contract. From phase 4 a `breaking` fragment
  goes with a raised contract revision, and the `openapi-diff` job fails a breaking diff that
  leaves the revision unchanged (ADR 0010). That job arrives with phase 4.
- **At release** `scripts/changelog.ps1 assemble` writes the fragments into `CHANGELOG.md` and
  deletes them. The release pull request is exempt from the check for that reason.

Details: [`changelog/unreleased/README.md`](../changelog/unreleased/README.md).

## Versioning

- The version is SemVer and starts below 1.0. `scripts/changelog.ps1 bump` derives the next
  version from the fragment kinds. Below 1.0: `breaking` or `feature` raises the minor, anything
  else the patch. From 1.0: `breaking` raises the major.
- The version is written in `<Version>` in the `src/` project files. The tag is `v` plus that
  version. *Enforced by* `release.yaml`, step "Validate release tag matches csproj version": the
  run fails when no `src/` project carries the tag's version.
- The contract revision is separate from the product version. It is an integer that every client
  compares with the API's before its first call; a mismatch stops `bf-scraper` and `bf` with exit
  code 5 (ADR 0007, ADR 0010). Arrives with phase 4.

## The prerelease gate

Before the release pull request is opened, the `prerelease-review` skill reads every change merged
into `develop` since the last release. It looks at concurrency, repeat-safe writes, the migration,
the API contract, security, and the fragments. Each finding becomes an issue. A finding labelled
`severity:blocker` stops the release. *Enforced by* the `release` skill, whose first step checks
for open blocker findings.

From phase 5 the gate includes a **scraper soak**: one full run of `bf-scraper` against the booking
site from the home machine, with the candidate build. From phase 6 it runs against the deployed
API. The exit code is the result (ADR 0007).

No workflow runs at this gate. `release.yaml` has no prerelease mode: it reacts to every published
GitHub release, a pre-release included.

## The release gate

A release is: the release pull request from `develop` into `main`, then a tag on `main`, then a
GitHub release published from the tag. The `release` skill prepares everything up to the tag. The
owner publishes the GitHub release.

`release.yaml` runs when the GitHub release is published. Today it is the pipeline the repository
template ships, written for a NuGet library:

| Job | What it does today |
|---|---|
| Validate Release Build | Checks the tag against `<Version>`; builds Release; runs all tests; applies the coverage gate |
| Pack & Validate NuGet | Runs `dotnet pack` on every project under `src/`; generates a CycloneDX SBOM when a package was produced |
| Verify Documentation Builds | Builds the docfx site without deploying |
| Publish to NuGet.org | Runs only when a package was produced |
| Build & Deploy Documentation | Calls `docfx.yaml` for the tag |
| Attach Artifacts to Release | Runs only when a package was produced |

**Barefoot is an application and publishes no NuGet package.** Before the first release with a
`src/` project, `release.yaml` must stop packing and publishing packages. That is a
configuration-only pull request and is not written yet. The `release` skill checks for it and stops
if it is missing.

Arrives with phase 6: building and publishing the container images for `Api` and `Web`, publishing
the three command-line hosts, and deployment. No workflow does any of these today.

**Third-party notices.** `THIRD-PARTY-NOTICES.md` at the repository root lists every NuGet package
distributed with Barefoot. `scripts/Generate-ThirdPartyNotices.ps1` writes it;
`scripts/Generate-ThirdPartyNotices.ps1 -Check` exits 1 when the committed file is out of date.
*Enforced by* `third-party-notices.yaml`, job `Third-Party Notices Check`, on every pull request,
and by the `release` skill, which runs `-Check` again.

## Scheduled workflows and failure reporting

| Workflow | Schedule (UTC) | What it does |
|---|---|---|
| `security-alerts.yml` | Daily 03:17 | One issue per open code-scanning, secret-scanning or Dependabot alert; closes the issue when the alert closes |
| `codeql.yaml` | Sundays 00:00 | CodeQL on `main` |
| `stryker.yaml` | Sundays 06:00 | Full mutation run; keeps one `kind:mutation-survives` issue current |
| `scorecard.yaml` | Mondays 06:31 | OpenSSF Scorecard; results go to code scanning |
| `license-audit.yaml` | Tuesdays 06:15 | License audit of `main` |
| `template-deps.yaml` | Mondays 07:00 | Its job runs only in the template repository and is skipped here |

**Rule.** A scheduled run that fails becomes an issue. A red run nobody opens is not a report.

**How.** `scripts/report-failure.ps1` keeps exactly one issue per workflow and failure signature:

- The issue body starts with a hidden marker, `<!-- automated-failure: <workflow>|<signature> -->`.
  The script finds the issue again by that marker.
- First failure: the script creates the issue with the failing output, the signature, a suggested
  fix and the run link. Labels: `automated`, `scheduled`, and the concern.
- Same failure again: the script adds a comment with the run link. It does not open a second issue.
- Next success: the script closes every open issue of that workflow with a comment naming the run.
- A cancelled or skipped run changes nothing.
- The signature is the text the caller passes. Without one it is a hash of the failing output with
  timestamps, identifiers and numbers removed, or `default` when there is no output.
- `-Summary` keeps one issue listing the automated issues open longer than seven days. An issue
  labelled `critical` and open longer than two days puts `HIGH PRIORITY` in the title.

The script reads and writes issues and labels only. It never edits a workflow or any other
protected file. A person or a session fixes the cause through a normal pull request.

**Enforced by** `report-scheduled.yaml`. It starts when one of the scheduled workflows completes
(`codeql.yaml`, `license-audit.yaml`, `scorecard.yaml`, `security-alerts.yml`, `stryker.yaml`), acts
only when that run was started by its schedule, and passes the run's conclusion to the script. It is
the only workflow with `issues: write` for this purpose, it checks out nothing but the script from
`main`, and the five workflows themselves are unchanged. A run that timed out or failed to start
counts as a failure. Workflows that complete and are triggered this way run from `main`, so the
script and `report-scheduled.yaml` must both be on `main` before a failure is reported. Until then
a scheduled failure is only a red run.

`scripts/report-failure.Tests.ps1` holds the Pester 5 tests for the script. No workflow runs them
yet.

**The semiweekly job** on the home machine (refresh the catalog, run, send digests) arrives with
phase 6. Its steps are gated on the shared exit codes: stop on 5, 7, 8 and 9, continue past 2 and 3
(ADR 0007). It reports through the same script with the exit code as the signature, for example
`-Workflow semiweekly-job -Signature exit-7`, so each kind of failure has one issue.

## Supply chain

| Rule | Enforced by |
|---|---|
| Every action is pinned to a commit SHA | `actions-audit.yaml` (zizmor); Dependabot updates the pins weekly |
| Every workflow declares `permissions:` | `actions-audit.yaml` (zizmor), Scorecard |
| A CycloneDX SBOM exists for every pull request and every push to `main` | `sbom.yaml` |
| NuGet packages, actions and the Python tools are updated weekly | `.github/dependabot.yml` |
| Tools used by CI are pinned | `.config/dotnet-tools.json`, a protected file |

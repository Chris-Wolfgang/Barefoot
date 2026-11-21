---
name: release
description: Prepare a Barefoot release. Checks the preconditions, derives the version from the changelog fragments, assembles the changelog, writes the upgrade notes, regenerates the third-party notices, and opens the release pull request from develop into main. Stops before the tag. The owner tags and publishes the GitHub release.
---

# Release

This skill prepares everything up to the tag. It does not push a tag and does not create the
GitHub release: the owner does both. See
[docs/DELIVERY-PROCESS.md](../../../docs/DELIVERY-PROCESS.md).

`X.Y.Z` below is the version from step 2.

## 1. Preconditions

Check each with a command and print the output. Stop at the first one that fails.

1. **The prerelease review covers the head of `develop`.** The summary comment names the commit it
   covered; `git rev-parse origin/develop` after `git fetch --prune origin` prints the same commit.
2. **No open blocker.**
   `gh issue list --label prerelease --label severity:blocker --state open --json number,title`
   prints `[]`.
3. **`develop` is green.** Every check of the latest run for that commit passed. An empty or
   partial answer from the API is unknown: ask again, do not read it as passing.
4. **`develop` contains `main`.** `git log --oneline origin/develop..origin/main` prints nothing.
   If it prints commits, merge `main` into `develop` first; that merge needs no pull request.
5. **No protected file differs between `main` and `develop`.**
   `git diff --name-only origin/main...origin/develop` lists none of the protected files
   (DELIVERY-PROCESS, "Protected files"). If it does, the release pull request would fail
   `Protected Files Guard`. Take those files to `main` first in a configuration-only pull request,
   merge `main` into `develop`, and start again.
6. **`release.yaml` will not publish a NuGet package.** For every project under `src/`,
   `dotnet msbuild <project> -getProperty:IsPackable` prints `false`, or `release.yaml` no longer
   has the `Pack & Validate NuGet` and `Publish to NuGet.org` jobs. If neither holds, stop: a
   published GitHub release would push Barefoot's projects to NuGet.org.

## 2. Version

`pwsh ./scripts/changelog.ps1 bump` prints the next version from the fragment kinds. Below 1.0:
`breaking` or `feature` raises the minor, anything else the patch. From 1.0: `breaking` raises the
major, `feature` the minor.

Write the version into `<Version>` of every project under `src/`. `release.yaml` fails when no
`src/` project carries the tag's version.

From phase 4: if any fragment is `breaking`, confirm the contract revision was raised in the same
cycle and that `Api`, `bf-scraper` and `bf` in this release are built against the same revision.
A mismatch stops the semiweekly job with exit code 5.

## 3. Changelog and upgrade notes

- `pwsh ./scripts/changelog.ps1 assemble -Version X.Y.Z` writes the new section into `CHANGELOG.md`
  and deletes the fragments. Read the result as release notes and fix the wording where it reads
  like a commit message.
- Add an "Upgrading" paragraph under the new section when any of these changed. Say what the
  operator does, in order:
  - a migration: what it changes, whether `Down` loses data, and the command (`bf-maint migrate`);
  - the contract revision: the API and both hosts are updated together;
  - an exit code or a verb of `bf-scraper`, `bf` or `bf-maint`: what the scheduled task must change;
  - a setting: its key, its default, and whether the Settings page or bootstrap configuration
    holds it.
- Regenerate `docs/EXIT-CODES.md` and `docs/api/openapi-v{n}.json` if their tests say they differ.
  Both arrive with their phases (5 and 4).

## 4. Third-party notices

```
pwsh ./scripts/Generate-ThirdPartyNotices.ps1
pwsh ./scripts/Generate-ThirdPartyNotices.ps1 -Check
```

Commit `THIRD-PARTY-NOTICES.md` if the first command changed it. The second command must exit 0.

## 5. Upgrade check (from phase 6)

Before phase 6 nothing is deployed; write "not applicable: nothing is deployed" in the pull request.

From phase 6, with the candidate build:

1. `bf-maint migrate --status` against the deployed database shows the expected pending migrations
   and nothing else.
2. Apply the migration to a copy of the database. Record the duration.
3. Run the scraper soak from the `prerelease-review` skill against the candidate API.
4. Confirm that searches, results, settings and stored digests from before the upgrade are still
   readable.

Record each result in the release pull request.

## 6. The release pull request

1. Steps 2 to 4 are one pull request into `develop`, branch `release/X.Y.Z`, title
   `release: X.Y.Z`. It deletes fragments and changes `CHANGELOG.md`, so the changelog check
   exempts it. Do not add `no-changelog`.
2. After it merges, open the pull request from `develop` into `main`, title `release: X.Y.Z`. The
   body holds the new changelog section, the "Upgrading" paragraph, the results of steps 1 and 5,
   and the list of pull requests it carries (`git log --oneline origin/main..origin/develop`).
3. Wait for every check on its latest commit. The owner merges it. The ruleset allows squash or
   rebase only.
4. After the merge, merge `main` into `develop` so the next cycle starts from the release commit.

## 7. Tag and GitHub release: the owner

Hand over, do not do:

- the commit on `main` to tag, and the tag name `vX.Y.Z`;
- the new changelog section as the release body.

The owner pushes the tag and publishes the GitHub release. Publishing starts `release.yaml`.

## 8. After the release

- Watch the `release.yaml` run for the tag: tag and version check, Release build, tests, coverage
  gate, documentation build and deploy. Report each job's result from that run. A job that was
  skipped is reported as skipped, with its condition.
- From phase 6: confirm the deployed API's `GET /version` reports the released build and revision,
  and that the next semiweekly run ends with exit code 0 or 2.
- Issues that the release closes: draft the closing comment with the evidence (the pull request,
  the commit on `main`, the run) and wait for the owner to say "close". Rolling maintenance issues
  stay open and get a summary comment.
- Move open issues that missed the release to the next phase label with a comment.

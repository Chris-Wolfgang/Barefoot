---
name: prerelease-review
description: Full review of everything merged into develop since the last Barefoot release, run before the release pull request is opened. Covers concurrency, repeat-safe writes, the migration, the frozen API contract and its revision, security, the scraper soak, exit codes, docs and fragments. Findings become issues, not notes.
---

# Prerelease review

Run this when `develop` is a release candidate, before the `release` skill opens the release pull
request. See [docs/DELIVERY-PROCESS.md](../../../docs/DELIVERY-PROCESS.md).

**Input.** Every pull request merged into `develop` since the last release tag, and the fragments
under `changelog/unreleased/`. List them with a command and print the list before reading:

```
git fetch --prune origin
git log --oneline <last-tag>..origin/develop      # no tag yet: origin/main..origin/develop
```

**Output.** One issue per finding, labelled `prerelease` and a severity (`severity:blocker`,
`severity:major`, `severity:minor`), and one summary comment on the release tracking issue. Create
a label that does not exist yet.

Items 3 to 8 apply only once their phase has merged (DESIGN §13). For an item that does not apply,
write "not applicable: phase N has not merged" in the summary. Do not skip it silently.

## Read every change for

1. **Layer rules.** No new reference crosses a line in DESIGN §2: `Scraper`, `Admin` and `Web`
   without `Data` or EF Core; `Admin` without `Obe`; `Maintenance` without `ApiClient` or `Obe`;
   `Api` without `Obe`; `Core` and `Presentation` without a framework. Confirm the architecture
   tests ran in the last green run and that each has its positive control.

2. **Quality bar.** On the head of `develop`: coverage at 95 % on every `src/` assembly and 100 %
   on every test assembly, no skipped test without an open issue, every test project with a
   `stryker-config.json` at or above its `thresholds.break`. Read the numbers from the latest run
   for that commit. A result that came back empty is unknown, not passing.

3. **Concurrency** (from phase 3). Every read-modify-write on a shared row: is the update
   conditional on `Version`? The run lock: can two runs be in progress? Leases: can two workers
   hold the same request or flight set, and does a late result with an old `attempt` change
   anything? Is every retry bounded?

4. **Repeat-safe writes** (from phase 4). Every endpoint the scraper or `bf` can retry (`claim`,
   `result`, `complete`, `finish`, `abort`, `digests/send`, catalog snapshots): the same request
   twice gives the same outcome and one side effect. `digests/send` twice mails nobody twice.

5. **The migration** (from phase 2). Until the first release: `InitialCreate` matches the model and
   `bf-maint verify-schema` agrees. After it: each new migration applied to a copy of the Azure SQL
   database with real row counts. Record the duration and the locks, the nullability of new
   columns against existing rows, and a `Down` that round-trips. Check that it still runs on SQLite
   in the tests. Remember the database sleeps: a long migration must survive a cold start.

6. **API contract** (from phase 4). Diff `docs/api/openapi-v{n}.json` against the last release.
   For every removed or renamed member, tightened validation or changed status code: is the
   contract revision raised, and is there a `breaking` fragment that names the change? Is any
   version in `docs/api/frozen.txt` touched? Does `GET /version` report the revision the clients
   were built against? A mismatch stops `bf-scraper` and `bf` with exit code 5 on the next
   semiweekly run, so the API and both hosts are released together.

7. **Exit codes and the job gate** (from phase 5). `docs/EXIT-CODES.md` equals the `ExitCode` enum.
   Each new code is on the right side of the semiweekly job's gate (stop on 5, 7, 8, 9; continue
   past 2 and 3). stdout of each verb is still only the verb's output.

8. **Scraper soak** (from phase 5). On the home machine, with the candidate build, run the job's
   steps once against the booking site:

   ```
   bf-scraper refresh-airports
   bf-scraper refresh-resorts
   bf-scraper run
   bf send-digests          # with the file-sink mail sender until phase 6
   ```

   Before phase 6 the API is a local instance; from phase 6 it is the deployed one. Record for each
   step the exit code, and for the run: lookups done and failed, seconds per lookup, whether the
   breaker tripped. Pass: every step ends with 0 or 2, the breaker did not trip, and every failed
   lookup has a cause that is not a parser defect. A selector or price-text change on the site is a
   finding with a new fixture, not a retry. Run the soak once per candidate. Keep the politeness
   delay; never shorten it to finish sooner.

9. **Security.** Every new endpoint declares a permission. No secret, address or digest body is
   logged above Debug. Every new dependency has an allowed license and is in
   `THIRD-PARTY-NOTICES.md`. Code-scanning, secret-scanning and Dependabot alerts are at zero or
   each has a written disposition. Enumerate the open alerts with a command; do not count from
   memory.

10. **Users cannot see each other.** For every new query, endpoint, page and digest template: does
    the visibility predicate scope it to the caller's own searches? Does any response, error text
    or digest reveal that another user exists?

11. **Docs against merged work.** Each issue closed in this cycle has its behaviour documented
    where a user or an operator would look. DESIGN.md and the ADRs still describe what the code
    does. From phase 7: every changed screen passes the axe scan in light and dark, and its mockup
    README entry is current.

12. **Fragments.** Every fragment reads as a release note a user understands. Types are right.
    Every merged pull request that changed `src/` has a fragment or carried `no-changelog` with a
    reason.

13. **Scheduled checks.** List the open issues labelled `automated`. Each is fixed, or has a reason
    to ship with it written on the issue.

## Then

- A finding that can be fixed forward goes in as a pull request to `develop` before the release
  pull request is opened. Anything else is an issue with a severity label.
- The release does not start with an open `severity:blocker` finding. The `release` skill checks.
- Post the summary comment: what was read (the printed list), what was found (issue links), what
  was deferred and why, and for each numbered item above "checked", "not applicable" or "not
  checked" with the reason. State the commit of `develop` the review covered. Any commit merged
  after it needs the review repeated for that commit.

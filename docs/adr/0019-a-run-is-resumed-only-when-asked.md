# ADR 0019 — A run is resumed only when asked

- **Status:** Accepted, 2026-11-27. Adds to [ADR 0003](0003-api-owns-the-run-lifecycle.md) (resume
  semantics) and [ADR 0007](0007-three-hosts-scraper-admin-maintenance.md) (`run [--resume]`). It
  closes the point [ADR 0017](0017-answers-to-the-blocking-questions-of-2026-11-27.md) left open
- **Deciders:** the owner (points 1 to 3); the builder (points 4 to 8, marked, for the owner's review)
- **Scope:** `Business` (run lifecycle), `Api`, `bf-scraper`, the scheduled job

## Context

A run can end without finishing: the breaker trips and the scraper aborts it, or the worker crashes
and its leases expire. ADR 0003 gives the API "resume semantics" and ADR 0007 gives the scraper
`run [--resume]`, but no record said when a run is resumed, and by whom. ADR 0017 named this as its
one open point.

## Decision

The owner's answer:

1. **`bf-scraper run --resume` resumes the last run when that run did not succeed.** It does the
   lookups that run still lacks.
2. **`bf-scraper run --resume` after a run that succeeded does nothing** and exits `Ok` (0), saying
   that there is nothing to resume.
3. **`bf-scraper run` without `--resume` always starts a new run**, whether or not the last run
   succeeded.

What follows from it, chosen by the builder:

4. **"Did not succeed" is every status but `Complete`**: `InProgress` (the worker crashed),
   `Incomplete` and `Failed`.
5. **Only the last run can be resumed.** Once a new run has started, an older unfinished run stays
   as it is and is never worked on again.
6. **A new run closes an unfinished one.** `POST /runs` no longer answers `409` because an earlier
   run is still `InProgress`: it sets that run to `Incomplete` and starts the new one. It answers
   `409` only while the earlier run holds a lease that has not expired, which means a worker is
   working on it at this moment.
7. **There is no time limit on resuming.** An old run can be resumed as long as no newer run exists.
8. **The scheduled job runs `bf-scraper run`, without `--resume`.** Each scheduled day gets a new
   run. Resuming is something a person does, or a later change to the job.

`--resume` with no run at all in the database also exits `Ok` (0).

## Not chosen

- *The next scheduled job always finishes the old run first.* The owner chose the explicit option:
  what a job does should not depend on how the last one ended.
- *A resume window (24 hours was proposed).* The owner's answer names none. Point 5 already ends
  the life of an old run when the next one starts.
- *A new status "Abandoned" for a run closed by point 6.* `Incomplete` says the same thing: some
  lookups were not done.
- *An error exit for `--resume` with nothing to resume.* The owner's answer is success.

## Consequences

- `POST /runs/resume` keeps its shape (ADR 0003): the run to resume, or `404` when the last run is
  `Complete` or there is none. The scraper turns that `404` into exit 0.
- `POST /runs` changes: the `409` rule in `DESIGN.md` §"Runs" is narrower (point 6). The document is
  updated with the code, in phase 3.
- Prices from a run resumed days later mix two dates. Each result carries its own time, so the
  digest can say so; nothing prevents it.
- A run closed by point 6 keeps the results it has. Whether its digests are still stored and sent
  is decided with the run lifecycle (Barefoot#75, Barefoot#80).
- After a tripped breaker (exit 3) the scheduled job goes on to `send-digests` as before, and the
  next scheduled day starts a new run. The unfinished one is resumed only if someone runs
  `bf-scraper run --resume` before then.

# ADR 0003 — The API owns the run lifecycle; the scraper pulls work

- **Status:** Accepted, 2026-11-06 (amended 2026-11-06: attempt-token semantics; 2026-11-20: no
  idempotency header anywhere, "one transaction" means one `SaveChanges`)
- **Scope:** `Business` (run lifecycle), `Api` (run resources), `Scraper`

## Context

A run is a sequence of lookups against the booking site, performed slowly and politely by one
scraper on a home machine, whose results many users' searches share. The database is behind an
API in Azure (ADR 0002), so every save is a round trip over the internet. The questions are where
the lifecycle lives (lock, explode, dedupe, lease, persist, aggregate, status, resume) and how a
worker that may crash, lose its network or be restarted mid-run interacts with it.

The earlier tool ran the whole lifecycle inside the scraping process with a file lock and a dozen
save points; its lesson is that checkpoint atomicity is hard when the worker owns the transaction,
and that a second worker needs a new design.

## Decision

Split by *what the decision is about*:

- **Data decisions → API/Business:** run creation; the lock (a filtered unique index on
  `Runs(Status) WHERE Status = InProgress` — the database guarantees one live run); explode and
  dedupe (ADR 0004); leasing requests and flight sets to a worker with an expiry; persisting
  results; vocabulary discovery; stage advancement; aggregation and digest storage; status
  derivation; resume semantics; recording an abort.
- **Site decisions → Scraper:** Playwright, politeness delay and jitter (the values come with the
  run, ADR 0011), per-item retries, the circuit-breaker decision, time-zone arithmetic on scraped
  times.

The scraper is a pull loop over `claim` / `result` for requests, then for flight sets, then
`complete` per search and `finish` (resource list in DESIGN §5). Leases are ~5 minutes; a crashed
worker's items return to Pending on expiry. Claims hand out one request at a time (run order,
cheap abort). Completion (aggregate + store digest) is an explicit call from the scraper, never a
side effect of a `result`.

**Attempt token = idempotency key.** A `claim` returns an `attempt` token (an opaque `Guid` stored
on the row); the matching `result` carries it in its body. There is no idempotency header: every
write is safe to repeat because of its own state (ADR 0012 §4), and for a `result` the row itself is
the record. The first `result` for an attempt is stored with a hash of its body; a repeat with the
same body returns the same response; a repeat with a different body is `422`; a `result` for an
expired or superseded attempt is `409 run.attempt_superseded` and ignored. The check and the write
are one `SaveChanges` under the retrying execution strategy (ADR 0001 §1) — no transaction is
wrapped by hand — and the row's `Version` token makes a retry racing the original lose.

## Not chosen

- *Scraper drives, API is a remote DAL.* Minimal code, but chatty at every save point, keeps the
  lock machine-local and puts the lifecycle in the one process that is supposed to be "just a
  scraper".
- *Automatic completion when the last result lands.* Puts aggregation (and formerly e-mail) inside
  a `result` request; a slow aggregate would stall the scraper's call.
- *A message queue.* Another service to run for one worker; the table *is* the queue, and the lease
  columns give the same semantics with one less moving part.

## Consequences

- `RunLifecycle` (Business) and `ScrapeLoop` (Scraper) are separate, separately tested classes;
  the lifecycle's checkpoint tests run server-side over SQLite.
- Checkpoint atomicity is one `SaveChanges` per `result`.
- A run knows its complete work list at creation (requests and flight sets are rows), so progress
  is countable and the UI can show a run in flight.
- ~2 HTTP calls per lookup; ~200 lookups a run is under a minute of overhead.
- A second scraper host needs no new design; the lease columns already carry the worker id.
- The scraper needs the same retry posture against a cold API that ADR 0001 gives the API against a
  cold database.

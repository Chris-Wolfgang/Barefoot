# ADR 0006 — E-mail: store the digest on completion, send in a separate step, owner-only recipients

- **Status:** Accepted, 2026-11-06 (amended 2026-11-20: where extra recipients are stored, who is
  told about a failed delivery, `digests/send` answers synchronously)
- **Scope:** `Business` (digest building and delivery), `Api` (`/digests`), `Admin` (`send-digests`)

## Context

Each search produces a digest per run, read on a phone by a user who may never open the web UI.
With several unrelated users, a free-text recipient list would make the server an open relay; a
synchronous send from inside the run would stall the scraper's call on a slow SMTP server; and mail
to a dozen strangers' Gmail/Outlook inboxes from a placeholder address lands in spam. The earlier
tool did all three; each is designed out here.

## Decision

1. **Store, then send.** `complete` builds the digest (HTML + plain text) into a `DigestDelivery`
   row (status *Pending*) and sends nothing. `POST /runs/{id}/digests/send` — the last job step,
   from `bf` — mails stored bodies and records *Sent* / *Failed*, attempts and error. The call is
   synchronous: it answers `200` with the counts sent and failed once every *Pending* row has been
   tried (a dozen users' mail is seconds of work), and a repeat finds nothing *Pending* and sends
   nothing. Everyone's mail goes out together after the run is known-good; the stored body is
   exactly what was sent; moving the send to its own workflow later is a scheduling change
   (ADR 0007).
2. **Recipients:** the owner's sign-in address, always; Admin-added extras; verified self-service
   extras ("add my spouse": confirmation link, cap, stored consent) in phase 2. Recipients are
   Admin-managed until then. Extras are stored once per owner and chosen per search:
   - **`Recipient`** is the owner's address book: `Id`, `OwnerId → AppUser`, `Address` (trimmed,
     lower-cased), `DisplayName`, `Version`; unique on `(OwnerId, Address)`; audited, so the trail
     names the Admin who added or changed it. An address is edited in one place and every search
     using it follows. The address may not be the owner's own sign-in address, and an owner has at
     most `digest.max_recipients_per_owner` of them (ADR 0011). The self-service columns
     (`ConfirmedUtc`, consent) arrive here in phase 2, so a spouse confirms once, not per search.
   - **`SearchRecipient`** links a search to the recipients it mails: `Id`, `SearchMatrixId`,
     `RecipientId`; unique on the pair. A search may use only its own owner's recipients (checked
     in `Business`). Foreign keys are `Restrict` like every other (ADR 0004 §12): deleting a search
     or a recipient removes its link rows in the same operation.
   - **`DigestDelivery.RecipientUserId` is nullable** — set for the owner, empty for an extra — and
     `RecipientAddress` is a copy taken when the digest is stored, with no key to `Recipient`, so
     editing or removing a recipient never rewrites delivery history.
   - Address-book endpoints live under the user (`/users/{id}/recipients`): the owner may read
     their own, only an Admin may write (`Permission.RecipientsWrite`). A search's chosen
     recipients travel with the search (`recipientIds`), and only a caller holding that permission
     may change them. A duplicate address is `409`; over the cap is `422`.
3. **Protection rules:** one message per recipient, never CC/BCC across users; no cross-user
   content in any template; addresses and bodies never logged above Debug; failure notices
   (abort reasons, run details) to the Admin only — a user's digest says "3 of 14 lookups did not
   complete", never why. A failed delivery raises an Open issue for the search's owner, whether
   the address that failed was theirs or an extra's (ADR 0008).
4. **Sender:** interim, a **dedicated mailbox** owned by the Admin (not the Admin's personal
   mailbox — a leaked app password must expose digests, not a life), 2FA + app password, credentials
   in Container Apps secrets / Key Vault, TLS. Target: Azure Communication Services Email or a
   transactional provider on an owned domain with SPF/DKIM (DMARC `p=none` to start). One
   `ISmtpClient` implementation either way; a file-sink implementation for local runs.
5. **Views:** Admin sees every delivery and status; a user sees "my digests" rendered from the
   stored HTML, each linking to its results. `resend` is an Admin action.
6. **Markup is mobile-first** (DESIGN §9): one card per room, 600 px single column, inline CSS,
   plain-text part always, deep links to the phone-contract screens; a fixture test pins it.

## Not chosen

- *Send inside `complete`.* Couples the scraper's call to SMTP latency and makes a mail failure
  look like a run failure.
- *Free-text recipient lists per search.* An open relay with extra steps; also the thing that puts a
  stranger's address in another user's search.
- *An address stored on each search.* One table fewer, but a changed address has to be corrected on
  every search that uses it.
- *Every digest to the owner's whole address book.* No link table, but the Jamaica search could not
  go to one person and the Bahamas search to another.
- *The Admin's personal mailbox as sender.* Blast radius.

## Consequences

- `DigestDelivery` (~30 KB a row) grows ~150 MB a year at a dozen users × twice a week × a few
  searches; a `digest.retention_days` setting comes later, together with the purge that reads it
  (ADR 0011).
- `MatrixRunStage.Aggregated → Emailed` happens in a different step from `complete`; resume logic
  treats "aggregated, not sent" as a pending delivery, not a pending search.
- `bf send-digests` must be idempotent (it only ever sends *Pending* rows).

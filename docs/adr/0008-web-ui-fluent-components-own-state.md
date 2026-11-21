# ADR 0008 — Web UI: Blazor Web App on Fluent UI Blazor; components own their state; role-scoped IA

- **Status:** Accepted, 2026-11-06 (amended 2026-11-06: vocabulary, disclaimer, Open issues, locked screens; 2026-11-20: screen error states, accessibility acceptance, when the UI polls, the version-mismatch page, who can own a search, the remaining screens locked)
- **Scope:** `Wolfgang.Barefoot.Web`, `Presentation`, the digest template

## Context

Up to a dozen unrelated users on laptops, with digests read on phones, need a hosted UI with no
install. The team is one developer on .NET; the UI must be accessible, themed light/dark, and
cheap to host (scale to zero). The earlier tool's desktop UI is the reference for *what* the
screens do, not for how they are built: its MVVM plumbing, its two-way-binding cascades and its
message-box error model have no web equivalent, and its accessibility, theme and trademark
findings are designed out here.

## Decision

1. **Blazor Web App, `InteractiveServer` globally**, as its own container beside the API; both
   scale to zero. Co-hosting in one process is a later optimisation if cold starts annoy.
2. **Fluent UI Blazor** as the component library: accessible by default, a neutral
   `FluentDesignTheme` with light/dark, a data grid that sorts, pages and virtualises. Charts are
   our own SVG (`ChartScaler`); no chart dependency. Runner-up kept in reserve: Bootstrap +
   QuickGrid (same `EditForm` code underneath). MudBlazor rejected on look and weight; Radzen on
   vendor shape.
3. **Components own their state.** No MVVM layer. The behaviour lives in framework-free classes in
   `Presentation` — `SearchDraft` (load/save mapping, window-mode rules, picker and chip logic,
   fan-out wiring), the pickers, `ChartScaler`, `PriceHistoryRange`, a paging helper — unit-tested
   with no UI framework. Screens call the Core interfaces through `ApiClient`. Every routable page
   is under the gated layout or carries an `[Authorize]` policy, checked by a test.
4. **Vocabulary.** The UI noun is **Search** (code: `SearchMatrix`); individual scrapes are
   **lookups**; a run's outcome for a search is *Complete / In progress / Incomplete / Failed*.
   Runs are never shown to Editors as objects.
5. **Information architecture by role** (DESIGN §12): an Editor's world is their searches; an
   Admin's is the system. Runs, reference data, deliveries, users and failure details are
   Admin-only; Editors see per-search outcomes. Admin is also an Editor ("mine" by default).
   Viewer is kept for v1; the Admin creates a Viewer's searches, and can pick as owner only someone
   who has signed in once and accepted the disclaimer (ADR 0004 §8).
6. **First sign-in disclaimer**, accepted once per user per disclaimer version
   (`AppUser.DisclaimerVersionAccepted`); Decline signs out. Re-shown only when the text version
   changes.
7. **Open issues.** Things that need the user (a failed digest delivery to them, a one-time window
   that passed, a resort offline in their search) are per-user rows with a `ResolvesWhen`
   condition. New ones pop up after sign-in; acknowledging stops the pop-up; the issue stays on the
   Dashboard until the condition clears. Admin-only issues (deliveries failed for all users) use
   the same model.
8. **Price history** is strict (own captures) for Editors and cross-user for Admin in v1; the v2
   priority is a shared market view in **cost per adult per night**, per resort and room level,
   weekly buckets — exact while the brand is adults-only.
9. **Mobile:** design target laptop/tablet; the search editor is desktop-first; the *phone
   contract* is sign-in, Dashboard, My digests and single-search Results. The digest itself is
   mobile-first (one card per room, 600 px single column, inline CSS, plain-text part, deep links
   to the phone-contract screens).
10. **Admin dashboard carries budget and health**: database status and free allowance remaining,
    storage, Container Apps compute vs grant (lagging), digests sent, last-run statistics, scraper
    last seen. Azure Monitor cards follow the managed-identity wiring.
11. **Readiness** (warming up / quota / missing migration / newer schema) is a full-page state
    before the shell, and so is a **version mismatch** between the web host and the API
    (ADR 0010 §8). Live run progress is polled every ~30 s, **only by a page whose last load showed
    a run in progress**; a page that saw none does not poll, because a poll is a query and would
    hold the serverless database awake (ADR 0001). Nothing else in the UI polls.
12. **Locked screens** (mockup `docs/mockups/v2/`, 2026-11-06): disclaimer + attention pop-up,
    Editor Dashboard, Admin System dashboard, Searches list, Search editor, Results. Locked
    2026-11-20: My digests and the e-mail, Profile, Price history, Runs, Deliveries, Reference
    data, Users, Settings, and the startup states. Their specifics are in DESIGN §12 and the mockup
    README. Two rules came out of drawing them: a table never leaves its card (text wraps, a note
    sits under its row, the less important columns go first on a narrow window, and Results drops
    none), and long lists are found, filtered and paged.
13. **Screen errors render in place.** Each screen has Loading, Loaded, Empty and Failed states; an
    `ErrorBoundary` in the gated layout keeps the shell alive for anything unexpected; errors are
    never modal (DESIGN §12).
14. **Accessibility is accepted per screen**, WCAG 2.2 AA, by an automated axe scan in both themes
    plus a keyboard pass — "accessible by default" in item 2 is the starting point, not the
    evidence (DESIGN §14).

## Not chosen

- *Blazor WebAssembly.* Token in the browser, a larger download, and no server-side token
  acquisition; `InteractiveServer` keeps the API token on the server.
- *A desktop app (WPF/MAUI).* An install per user on unknown machines; no phone story.
- *Binding Blazor to MVVM view models.* Setters that fire async loads cascade under two-way
  binding; `PropertyChanged` from thread-pool continuations needs per-screen marshalling.
- *MudBlazor / Radzen.* See 2.

## Consequences

- The neutral theme comes from the library. Accessibility starts there and is then accepted screen
  by screen (item 14), not assumed.
- The editor is the single largest UI task; the thin screens are a day or two each.
- A first-run tour/wizard is deferred to a future version; the empty Dashboard carries onboarding.

# ADR 0013 — Documentation site: keep DocFX and Pages, for operator pages

- **Status:** Proposed, 2026-11-20 (written with the phase 0 stack; Chris accepts or rejects it by
  merging or closing the pull request that carries it)
- **Scope:** `docfx_project/`, `docfx.yaml`, the documentation jobs of `release.yaml`, GitHub Pages

## Context

The repository template ships a documentation chain built for a NuGet library: a DocFX project, a
workflow that builds it and deploys a versioned copy to GitHub Pages at each release, and a version
picker. Barefoot is an application. The earlier tool carried the same chain half-configured: the
workflow existed, Pages was never enabled, and the pages were the template's placeholders
(sandals-search#260). A chain in that state costs attention on every release and documents nothing.

Barefoot has two kinds of documentation. *Engineering* documents — the design, the ADRs, the
conventions — are read by whoever changes the code, beside the code, in `docs/`. *Operator*
documents are read by whoever runs the thing: how to migrate the schema, what each readiness state
means and what to do about it, what an exit code says, how to set up the scheduled job. DESIGN §15
already assumes the second kind exists as DocFX pages, each the twin of an engineering document.

## Decision

1. **Keep the DocFX project and the Pages deployment.** The site is for the operator pages and the
   generated API reference. It is not a second home for the engineering documents.
2. **Engineering documents stay in `docs/`** and are read on GitHub. The site links to them; it does
   not copy them.
3. **An operator page arrives with the feature it describes**, in the same pull request, and is the
   twin of an engineering document: `docs/MIGRATE.md` and the site's *Migrate the schema* page say
   the same thing for two readers, and each says to keep the other in step. No operator page is
   written ahead of its feature.
4. **The template's placeholder pages are replaced now** by pages that say what Barefoot is, that
   it is not built yet, and where the design is. No page tells a reader to install a NuGet package.
5. **Enabling Pages is a repository setting and stays a manual step** (`scripts/Setup-GitHubPages.ps1`).
   It is done before the first release; until a release exists there is nothing to deploy, because
   the site is published per release tag.
6. **The generated API reference stays.** It is built from the XML documentation of the `src/`
   projects, which the build already requires. It serves whoever writes a client against `Core` and
   `Contracts`, and costs nothing to keep.

## Not chosen

- *Remove the DocFX project, the workflow and the release jobs.* It ends the half-configured state
  at once, and the operator pages would then be Markdown in `docs/`. Rejected because the operator
  of a deployed Barefoot is not always someone with the repository open, a versioned site answers
  "what does *my* version do", and the chain already works in the sibling repositories. It is the
  right answer if the operator pages turn out to be two pages nobody reads; removing the chain later
  is a small configuration-only pull request.
- *Move every document to the site.* Two copies of the design, or a design that is no longer beside
  the code it governs.

## Consequences

- The release jobs that build and deploy the site keep running. They need a `src/` project to build
  the API reference; until phase 1 the documentation workflow skips, as it does today.
- Before the first release someone enables Pages. The release skill lists it.
- Each of phases 2, 4, 5 and 6 adds operator pages; the pull-request checklist asks for the twin
  when an engineering document with an operator audience changes.
- If this ADR is rejected, the same pull request is replaced by one that deletes `docfx_project/`,
  `docfx.yaml`, the two documentation jobs in `release.yaml`, the `docfx` tool pin and the three
  DocFX documents under `docs/`.

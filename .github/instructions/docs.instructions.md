---
applyTo: "docs/**/*.md"
---

# Documentation conventions

The `docs/` tree is **audience-first** and builds into a DocFX site:

- **`docs/guide/`** — writer-facing: the overview and the script-language spec.
- **`docs/contributing/design-notes/`** — one design note per component and
  compiler stage, each recording the goal, key decisions, and tradeoffs. Notes
  are filed in a folder per area (`core/`, `runtime/`, `language/`,
  `configuration/`, `diagnostics/`, `cli/`, `visualization/<surface>/`, `other/`);
  the [reading guide](../../docs/contributing/design-notes/README.md) maps them.
- **`docs/api/`** — the generated C# API reference (do not hand-edit).

## Writing

- **American English**; keep prose tight; use sentence-style headings and a table
  of contents on longer notes.
- **Write loanwords unaccented** — `facade`, not `façade` — so the ASCII text
  matches the code and the spell checker.
- **Link to the authoritative doc** rather than restating build steps, conventions,
  or API details — point at `CONTRIBUTING.md`, the design notes, or the API
  reference so the docs never drift.
- Use **Mermaid diagrams** to clarify flow, architecture, and state; keep each
  diagram small and maintainable. Prefer a diagram over a long paragraph when it
  reads faster.
- **Polish the writing:** active voice, short paragraphs, concrete examples. Keep
  Markdown clean for `markdownlint` and links valid for `lychee`.
- A design note opens with a status callout (`> [!NOTE]` proposed / in progress /
  implemented) and is written as the current design, not a changelog.
- **Describe only what ships.** A user-facing page must not advertise a capability
  the system does not have, and a status callout states what is built now — a
  feature that is planned, dormant, or superseded says so.
- **Guide examples compile as written.** A tutorial that teaches a form the compiler
  reads differently is worse than no tutorial. Compile each example you add or change:

  ```bash
  dotnet run --project src/DialogueDown.Cli -- compile <example.md> --emit playbook
  ```

- **Reword rather than extend the dictionary.** When cspell flags a word you coined,
  a plainer phrase is usually clearer. Add a word to
  `cspell.json` only when it is a real name: a tool, a format, a proper noun.

## Design notes

- **One note per concept, named for the concept.** A follow-up to an existing
  design goes into that note's section; a new note is only for a concern the
  existing notes do not cover. Never name a note after the step that delivered it
  (`… Pass 2`, `… Follow-up`): those notes restate their parent and drift from it.
- **Present tense, no history.** A note records the design as it stands. Leave out
  issue and PR numbers, branch names, test counts, and delivery labels such as
  "Component 3"; git holds that history.
- **Link the shared pipeline diagram.** The reading guide owns the one diagram of
  the compiler stages; a note links it rather than drawing its own copy.
- **Links must resolve inside the site.** DocFX only follows links within `docs/`.
  Link a file outside it — `conformance/`, `CONTRIBUTING.md`, source code — by its
  GitHub URL (`https://github.com/pengzhengyi/dialoguedown/blob/main/<path>`), and
  link a section of the reading guide rather than a bare folder.

## How to add a design note

1. Create `docs/contributing/design-notes/<area>/<Note Name>.md` with a status
   callout and the note's goal, key decisions, and tradeoffs. Pick `<area>` from
   the folders above — the one whose reading guide section the note belongs to.
   Use `> [!NOTE]` for the neutral status line (e.g. "Status: **implemented**") —
   status is informational, not an alarm. Reserve `> [!IMPORTANT]`/`> [!WARNING]`
   for genuine caveats or hazards.
2. Add the note to the **reading guide** in
   `docs/contributing/design-notes/README.md`: put it in the section matching its
   folder, in reading order, and keep that section's Mermaid chart current.
3. Regenerate the site sidebar, `toc.yml`, from the reading guide. Never edit it by
   hand; CI fails when the two disagree:

   ```bash
   python3 .github/scripts/generate-design-notes-toc.py
   ```

4. Build the site to confirm it renders and links resolve:

   ```bash
   dotnet tool restore
   dotnet tool run docfx docs/docfx.json --warningsAsErrors   # add --serve to preview locally
   ```

   The build fails on a warning, and CI builds the site on every pull request, so a broken
   link or a missing cross-reference is a failure to fix rather than a note to leave.

## Moving, merging, or renaming a note

A note's path is cited outside `docs/`, so a move is not done until every citation
follows it. After updating the reading guide and regenerating `toc.yml`, search the
repository for the old file name and retarget:

- links in other notes, the guide, `README.md`, and `CHANGELOG.md`;
- the accepted pairs in `.github/scripts/find-doc-duplication.py`.

When a note is merged into another, carry every claim the merged note made that the
owner lacks before deleting it.

The generated `docs/_site/` and `docs/api/*.yml` are ignored — never commit them.

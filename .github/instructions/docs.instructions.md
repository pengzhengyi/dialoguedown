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
3. Register it in `docs/contributing/design-notes/toc.yml` so it appears in the
   site sidebar (`- name:` + `href:`), with the folder in the `href:`.
4. Build the site to confirm it renders and links resolve:

   ```bash
   dotnet tool restore
   dotnet tool run docfx docs/docfx.json           # add --serve to preview locally
   ```

The generated `docs/_site/` and `docs/api/*.yml` are ignored — never commit them.

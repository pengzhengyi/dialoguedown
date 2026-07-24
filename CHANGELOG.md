# Changelog

All notable changes to DialogueDown will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/), and this
project uses [Conventional Commits](https://www.conventionalcommits.org/) to keep
changes easy to categorize.

## Unreleased

### Added

- **Random choices** — a choice list whose options lead with a `` `%` `` weight now becomes a
  *random choice*: the engine picks one option at random by weight instead of offering the player a
  menu. Write an explicit percentage (`` `50%` ``) or a bare `` `%` `` to share the remaining
  chance equally, and the report's AST tabs show each option's weight. See the
  [Random Choice](docs/contributing/design-notes/Random%20Choice.md) note.
- **Autosave in the live visualization editor** — each editable document now has a persisted
  **Auto | Manual** save mode beside the Save button. Source defaults to Auto (saving 1s after you
  stop typing) and Config defaults to Manual, and explicit Save / <kbd>⌘/Ctrl-S</kbd> stay immediate
  in either mode. Saves are single-flight and generation-safe, so overlapping writes, stale
  recompiles, and cleared-but-unsaved edits can't happen. An accessible status reads
  Unsaved / Saving… / Saved, plus Conflict, Waiting for valid TOML, Saved — invalid TOML, and
  uncertain. External changes pause Auto in a conflict you resolve with Reload from disk or a
  confirmed overwrite; Config Auto validates the TOML before writing, while an explicit Config Save
  may persist invalid TOML. Navigation (tabs, node selection, Edit→View) flushes an Auto save and
  awaits it, or runs the Manual save-or-discard prompt. See the
  [Live Visualization — Autosave](docs/contributing/design-notes/Live%20Visualization%20-%20Autosave.md) note.

- **Diagnostics on the CLI** — `dialoguedown compile` now shows a script's problems and fails with
  a data-error exit code when it has errors, instead of silently succeeding. On a terminal each
  diagnostic is a rich block — the offending source line with a caret under it — and piped output
  is a greppable `file(line,column): severity CODE: message`. Each diagnostic is followed by a link
  to its entry on the hosted Error codes page. `--mode` chooses how far a
  compile proceeds after an error. The compiled result also exposes the located diagnostics
  (`CompilationResult.LocatedDiagnostics`) for other tools to render. See the
  [CLI Diagnostic Rendering](docs/contributing/design-notes/CLI%20Diagnostic%20Rendering.md) note.
- **Diagnostics overlay in the visualization editor** — the report's source editor now marks each
  of a script's problems in place: a squiggle under the offending text, a gutter marker, and a
  hover tooltip that explains it and links to its entry on the hosted Error codes page. The overlay
  refreshes on every recompile — save and hot-reload — and clears once the script is clean. It is
  built on a reusable, LSP-shaped diagnostic projection, laying the groundwork for a future language
  server and editor extension. See the
  [Diagnostics Overlay](docs/contributing/design-notes/Diagnostics%20Overlay.md) note.
- **Dialogue highlighting and grammar-correct completions in the visualization editor** — the
  report's source editor now colors dialogue as you read and write it — a speaker's name, `@id`,
  and `:` separator each distinctly, its tags, and jump indicators — over the Markdown, and its
  completions for speakers, `@id`s, `#tag`s, and jump targets are drawn from the compiler itself,
  so a suggestion can never be a name the compiler would reject. Both are projected from the
  compiler's own parse instead of a second grammar in the browser, sharing the LSP-shaped
  groundwork laid for a future language server. See the
  [Compiler-Projected Editor Semantics](docs/contributing/design-notes/Compiler-Projected%20Editor%20Semantics.md)
  and [Precise Speaker Tokens](docs/contributing/design-notes/Precise%20Speaker%20Tokens.md) notes.
- **Set the compilation mode per project** — a `dialogue.toml` can now choose how far a compile
  proceeds after an error (`mode = "stage-boundary"` or `"best-effort"`), and the visualization's
  Config tab shows, edits, and autocompletes it. The CLI `--mode` still overrides the project
  setting. See the
  [Compilation Mode Configuration](docs/contributing/design-notes/Compilation%20Mode%20Configuration.md) note.
- **The report reopens on the tab you left it on.** After a refresh, the visualization
  returns to the last tab you were viewing (Source, an AST stage, or the Semantic model)
  instead of always resetting to the Source tab.
- **Documentation link in the visualization and demo** — the report header now carries a link to the documentation site, and the live-demo landing page gains a Documentation button, so readers can get back to the docs from the hosted report.
- **Compiler pipeline behind one `IScriptCompiler` facade** — compiles a Markdown
  dialogue script through parse → transpile → desugar → semantic analysis: it builds
  a Dialogue AST, normalizes it (assembling jumps and filling default speakers), and
  resolves speakers, scenes, and jumps into a validated semantic model, reporting
  invalid references. Wire it up with `AddDialogueDown()` (DI) or
  `ScriptCompilerFactory.CreateDefault()`. See the
  [design notes](docs/contributing/design-notes/README.md).
- **Collect-and-continue diagnostics** — the compiler now reports every located problem
  it finds (errors and warnings) from a single compile instead of throwing at the first,
  recovering from each so later stages keep checking; a configurable compilation mode
  (`CompilerOptions.Mode`) decides how far a compile proceeds after an error — fail fast,
  stop at the stage boundary (the default), or run every stage — and results carry the
  collected diagnostics and `HasErrors`. Structural checks warn when a line carries more
  than one jump or when a choice branch reaches a fourth nesting level. See the
  [Diagnostics and Validation](docs/contributing/design-notes/Diagnostics%20and%20Validation.md)
  and [Choice Nesting Diagnostic](docs/contributing/design-notes/Choice%20Nesting%20Diagnostic.md)
  notes.
- **Configurable speakers, in code or from a `dialogue.toml`** — supply speakers (and
  mark one the default) through `CompilerOptions`, and the compiler binds them
  alongside a script's own: a configured default covers speaker-less lines when the
  script declares no `##default`, and a configured speaker unifies with a same-named
  script speaker. The `dialoguedown` CLI reads a project's `dialogue.toml` —
  discovered by walking up from the script, or named with `--config` — so `compile`
  and `visualize` honor it, and the `visualize` report autocompletes the configured
  speakers (even ones not yet used). Malformed configuration is reported with its
  source location. See the
  [design notes](docs/contributing/design-notes/README.md).
- **`dialoguedown` CLI** — `compile` runs the compiler pipeline; `visualize` opens
  the interactive report or writes a stage's graph as portable **Mermaid** or
  **Graphviz DOT** text (`--emit`).
- **Interactive `visualize` report** — explore each compiler stage as a graph — the
  Markdown, Dialogue, and Desugared ASTs, and the **semantic model** shown as a scene tree
  (each scene expandable to its script blocks, any node clickable for its source and preview)
  beside cross-linked, resizable speaker, anchor, and jump-resolution tables — plus a
  **Configuration** tab showing the applied `dialogue.toml` beside its configured speakers —
  with a runtime
  **View ⇄ Edit** toggle: a read-only, auto-updating **View** and an in-browser editor that
  saves back to the file — the dialogue **or** its `dialogue.toml`, which you can also
  **create** in place when a project has none — or edits the source behind any graph node in
  the inspector, or creates a new script from the launcher or a not-yet-existing
  path — or discards unsaved edits to restore the last saved version — with
  a compact find-and-replace panel, section folding, Markdown formatting shortcuts,
  document-aware autocomplete
  you accept with Tab or Enter, with an icon per symbol kind, synchronized
  editor/preview scrolling, and a
  **light/dark** theme.
- **`visualize` report navigation** — collapsible side panels, a full-screen mode,
  hover-to-spotlight a node's lineage (its ancestors and descendants), and per-graph
  position memory that keeps each stage's zoom, pan, and collapsed branches across
  tab switches and hot-reloads.
- **Error-code reference in the docs** — the hosted guide now lists every `DLG####`
  diagnostic the compiler reports, grouped by category with each message, severity, a
  plain explanation, and a worked before-and-after example that highlights the offending
  token and its fix. It is generated from the diagnostic catalog and its examples are
  compiler-verified, so the page never drifts from the code. See the
  [Error codes](docs/guide/error-codes.md) page.
- **Broken scripts still visualize what compiled.** When a script fails to compile past
  a stage, the report shows the stages it did produce and disables the ones it could not —
  each grayed tab noting it is unavailable because of compilation errors — instead of
  failing to open. See the
  [Unavailable Stage Tabs](docs/contributing/design-notes/Unavailable%20Stage%20Tabs.md) note.
- **Documentation site and live demo** — a
  [DocFX site](https://pengzhengyi.github.io/godot-dialoguedown/) (a writer Guide, a
  Contributing section with the design notes, and a generated C# API reference) and a
  [live, read-only demo](https://pengzhengyi.github.io/godot-dialoguedown/demo/) of
  the report, both published to GitHub Pages on every merge to `main`.
- **Development guardrails** — architecture tests that enforce the project's
  dependency direction (the engine-agnostic core stays free of the CLI, the
  visualization projects, and any engine/console packages), plus build-time size and
  complexity limits on the core library.
- **Project logo and favicon** — a chat-bubble Markdown "M" mark.
- Initial OSS community files and CI.

### Changed

- Development verification now provides faster targeted local loops and
  parallel, overlapped frontend CI while retaining the full test, analyzer,
  coverage, accessibility, and generated-bundle gates.
- `visualize <script>` now opens a **served session** (read-only **View** by default)
  instead of a one-shot static file; write an offline snapshot with `-o`.
- The `visualize` servers now compress responses (gzip), cutting the report's
  transfer roughly threefold over a LAN or VPN; the hot-reload stream stays
  uncompressed so events keep streaming.
- `SourceSpan` now allows a zero-width range (`SourceSpan.EmptyAt`, `IsEmpty`) so a
  synthetic node with no source text — such as a filled-in default speaker — marks a
  caret at its position instead of borrowing a neighbor's range.

### Removed

- The `--watch`, `--live`, and `--mode` flags — superseded by the default served
  session (View), `--edit` (start in Edit), and `-o` (static export).

### Fixed

- **Dialogue highlighting now reaches inside choices.** In the visualization editor, the
  jump arrow, tags, and code spans on a choice (a Markdown list item) are colored the same
  as at the top level, instead of being grayed out by the surrounding list styling.
- **The report now renders cleanly on small and phone-sized windows.** Every tab
  adapts below tablet width instead of overlapping, clipping, or running off-screen:
  the Source, Config, AST, and Semantic views stack their split panes with a handle
  to collapse the secondary pane, the footer wraps its controls, and the
  configured-speaker and node-details panels no longer overlap their neighbors.
- The brand mark now stays visible on dark backgrounds — it inverts to a light
  bubble across the report and launcher, the favicon, and the demo.
- Escaped inline punctuation (for example `\*`) no longer shifts the source spans of
  the text that follows it, so spans stay exact for diagnostics and the visualizer.

# Changelog

All notable changes to DialogueDown will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/), and this
project uses [Conventional Commits](https://www.conventionalcommits.org/) to keep
changes easy to categorize.

## [Unreleased]

### Added

- **Fixed End sentinel in the Source editor** — the compiler-projected `#END` target now appears
  in a read-only row below the source with an infinity marker; clicking it copies
  `[End](#END)`. It is reserved-target metadata, not a synthetic heading or source line, leaving
  the same panel seam for a future `#START` entry target once its semantics are defined. See
  [Progression order](docs/contributing/design-notes/Progression%20Order.md).
- **Zen mode in the report** — press `z` for a deeper full screen that also steps the active
  tab's side panel aside: the editor alone on Source and Config, the graph alone on the AST and
  Semantic Model tabs. `z` or `Esc` restores your layout exactly as it was. See the
  [Zen Mode](docs/contributing/design-notes/Live%20Visualization%20-%20Zen%20Mode.md) note.

- **Block conditionals** — group dialogue, commands, choices, and jumps into connected
  blockquote branches opened by `` `if` `` / `` `elseif` `` conditions and an optional
  `` `else` `` fallback. The compiler diagnoses severed or malformed branch chains, preserves
  nested blocks, and highlights marker keywords in the source editor. See
  [Block controls](docs/contributing/design-notes/Block%20Controls.md).
- **Jump from a graph node to its source** — the Node details panel now has a **Jump to source**
  icon beside the node title (on the AST graph tabs and the Semantic Model tab) that opens the
  Source tab with the node's text selected, so you can move from a node straight to the lines it
  came from. A synthetic node the compiler inserted (a filled-in default speaker) has no text of
  its own, so the jump places the cursor where it belongs instead.
  See the [Node Editing](docs/contributing/design-notes/Live%20Visualization%20-%20Node%20Inspector.md) note.
- **File Explorer in the report** — a served report opened through the launcher now shows a
  collapsible **Explorer** sidebar: browse the project's scripts as a tree, see the active one
  highlighted, and open another by clicking it or following a cross-file link. A VS Code-style
  header toolbar and right-click menus create files and folders and rename scripts and folders in
  place, and a pinned `dialogue.toml` entry opens the Config tab; switching scripts respects the
  save mode (Auto flushes, Manual prompts). See the
  [Live Visualization — File Explorer](docs/contributing/design-notes/Live%20Visualization%20-%20File%20Explorer.md) note.
- **Jump to a scene by typing `=>`** — the source editor now completes the whole jump
  target from the `=>` jump indicator: type `=>` and the report offers every scene by its
  heading; accepting one inserts a well-formed `[Heading](#slug)` with the heading as an
  editable field, so a dead link from a mistyped anchor is one keystroke to avoid. See the
  [Jump-Target Completion](docs/contributing/design-notes/Jump-Target%20Completion.md) note.
- **Quote and unquote blocks in the editor** — in Live Edit, `⌘/Ctrl-.` wraps every line the
  selection touches in a Markdown blockquote (nesting on an already-quoted line) and
  `⌘/Ctrl-Shift-.` removes one level; a right-click **surround menu** offers the same alongside
  bold, italic, and strikethrough, and the preview marks each nesting level with its own color.
  Handy for the blockquote-based block controls. See the
  [Live Edit](docs/contributing/design-notes/Live%20Visualization%20-%20Live%20Edit.md) note.

### Changed

- **Faster local .NET test feedback** — the documented contributor command and default VS Code
  test task now execute test projects with three MSBuild workers, reducing the measured warm
  local median by 28.8%. CI, release validation, builds, and coverage remain serial because their
  measurements did not improve.
- **One place to edit — the Source tab.** The graph tabs' node-details panel is now read-only
  on every tab, matching the Semantic Model tab, and **Jump to source** takes you to the node's
  text with its span selected. Editing a node in the side panel is gone: it duplicated the
  editor in a narrower space (selecting the root Document node rendered the whole file there),
  and only some tabs offered it. See the
  [Node Inspector](docs/contributing/design-notes/Live%20Visualization%20-%20Node%20Inspector.md) note.
- **Jump indicators use a preview ligature** — the Source tab and recognized
  jump syntax in node previews from Dialogue AST through Semantic Model show `=>` with a bundled
  Fira Code ligature, including parent Line/Document previews, while editors and underlying
  scripts keep the original two characters.
- **Semantic code-span colors in the Source editor** — commands, value queries,
  conditions, static random weights, and dynamic random weights now use distinct
  VS Code-inspired colors in light and dark themes, including inside blockquotes;
  block-control keywords and `#END` also use separate keyword and constant hues.
- **One unified report shell for `visualize`** — the standalone launcher picker page is gone.
  `visualize` (or `--pick`) now opens the report shell directly on an **empty state** — the
  Explorer over your project beside a "create your first dialogue file" call to action — and
  `visualize <script>` opens that same shell on your script, so the Explorer sidebar is available
  whichever way you start and there is a single page to learn. Serving a script that links images
  above its folder still resolves them with your consent (or an explicit `--root`). See the
  [Unified Served Shell](docs/contributing/design-notes/Live%20Visualization%20-%20Unified%20Served%20Shell.md) note.

### Fixed

- **The report on a narrow window or a phone** — the stage now keeps the room it needs. The
  stage tabs stay on one horizontally scrolling row instead of wrapping onto three, with the
  Zen and full-screen controls pinned beside them; the Explorer turns into a compact top
  section with a working collapse seam rather than an invisible one; and the expanded help
  panel is bounded so it can no longer paint over the editor, and on a short window it floats
  over the stage instead of being squeezed into an unreadable strip. Arrow buttons appear
  beside the tabs when they overflow, so a mouse without horizontal scrolling can still reach
  every stage. At 390px the stage went from a quarter of the window to just over half. See the
  [Narrow Screen Layout](docs/contributing/design-notes/Live%20Visualization%20-%20Narrow%20Screen%20Layout.md)
  note.
- **Graph zoom focus is visually lighter** — the editable percentage now uses a theme-accent
  underline instead of a rounded input focus ring inside the compact toolbar.
- **Diagnostic popovers stay visible at editor edges** — hover messages now overlay the report
  chrome instead of being clipped by the Source pane, remain within the browser viewport, and
  wrap to a compact reading width.
- **Source and preview stay aligned while scrolling** — scroll synchronization now anchors
  matching Markdown paragraphs, lists, blockquotes, and headings instead of only scene headings;
  front matter no longer creates a false heading that shifts every scene pair.
- **Block-control keywords keep their syntax color** — `` `if` ``, `` `elseif` ``, and
  `` `else` `` no longer fall back to the generic inline-code gray in the Source editor.
- **`ddown visualize` now stops on Ctrl+C** — the live server previously ignored the
  interrupt and had to be killed, because the shutdown signal reached the web host but
  not the command waiting on it. Pressing Ctrl+C (or sending a termination signal) now
  stops the session promptly, ending any open hot-reload stream instead of blocking
  shutdown.
- **Tab now indents in the Source editor.** In Live Edit, Tab indents at the start of a line
  or across a multi-line selection and inserts spaces mid-line (Shift-Tab outdents), like a
  normal code editor, instead of moving focus out of the editor; press Esc to move focus out.
  Tab still accepts an open autocomplete suggestion.

## [0.1.0] - 2026-07-28

### Added

- **Install the `ddown` command-line tool** — DialogueDown's CLI now ships as a
  cross-platform .NET global tool: `dotnet tool install --global DialogueDown.Cli`
  puts a `ddown` command on your `PATH` to `compile` and `visualize` scripts. The
  command is now named `ddown` (previously `dialoguedown`). See the
  [command-line guide](docs/guide/cli.md).
- **Styled speaker names are flagged** — when a line's speaker name is Markdown-styled
  (`*Alice*:`), it is not recognized as a speaker prefix and the line would otherwise be left
  silently unattributed; the compiler now warns and points at the fix — remove the styling to
  declare the speaker (`Alice:`). See the
  [Styled Speaker Prefix Diagnostic](docs/contributing/design-notes/Styled%20Speaker%20Prefix%20Diagnostic.md) note.
- **Searchable, sortable Semantic-tab tables** — the report's Speakers, Anchors, and Jump
  resolutions tables now filter as you type, sort on any column header, and offer a faceted filter
  for their categorical columns (a jump's Type, a speaker's Default), so a writer can find a
  speaker, scene, or jump and scan by name, level, or type. Filtering highlights the matched text
  in place and offers editor-style **Match case** and **Match whole word** toggles. Cross-linking,
  category accents, and the collapsible panels are unchanged.
- **Jump types in the Semantic tab** — the report's *Jump resolutions* table now leads with a
  color-coded **Type** column (Scene, End, Cross-file, Unresolved) so jumps group and read at a
  glance, prefixes a conditional jump with its guarding condition, and colors the *End* type with
  the same reserved hue as `#END` in the source editor.
- **End a run with `#END`** — divert to the reserved `#END` target
  (`=> [The end](#END)`) to stop the dialogue at a definite endpoint. `#END` is uppercase and
  reserved, so it never collides with a scene heading; the report highlights it as a reserved
  keyword and completion offers it as a jump target. Because a jump is non-returning, the compiler
  now warns when unreachable content — a trailing fragment or a second jump — follows a jump on the
  same line. See the
  [Progression Order](docs/contributing/design-notes/Progression%20Order.md) note.
- **Conditions** — put a game-state query with a `?` in front of a jump, a line, or a choice
  option (`` `"FoundKey"?` ``) to guard it: the jump fires, the line plays, or the option is
  offered only when the query reads as true, and otherwise it is skipped. A random option can
  lead with a condition before its weight, so a random pool can offer a dynamic set of options.
  The game answers each condition with a boolean; an unset one counts as false. See the
  [Conditional Jump](docs/contributing/design-notes/Conditional%20Jump.md),
  [Conditional Line](docs/contributing/design-notes/Conditional%20Line.md), and
  [Conditional Choice](docs/contributing/design-notes/Conditional%20Choice.md) notes.
- **Unquoted keys** — a condition and a dynamic weight may now drop the quotes around their key:
  write `` `IsAngry?` `` or `` `Luck%` `` instead of `` `"IsAngry"?` ``/`` `"Luck"%` ``. The
  trailing `?`/`%` marks where the key ends, so a key can read as a natural phrase and may contain
  spaces (`` `Is Alice happy?` ``). Quotes remain the escape for a key that must end in a literal
  `?`/`%`, and a value read (`` `"Alice.FavoriteColor"` ``) is still quoted. See the
  [Unquoted Keys](docs/contributing/design-notes/Unquoted%20Keys.md) note.
- **Copy a scene heading's anchor from the report** — hovering a heading in the live
  visualization's Source preview reveals a link icon that copies a ready-to-paste jump
  `[Heading](#slug)`, and the editor shows the bare `#slug` anchor on the heading line you're on.
  See the
  [Live Visualization — Heading Anchors](docs/contributing/design-notes/Live%20Visualization%20-%20Heading%20Anchors.md) note.
- **Random choices** — a choice list whose options lead with a `` `%` `` weight now becomes a
  *random choice*: the engine picks one option at random by weight instead of offering the player a
  menu. Write an explicit percentage (`` `50%` ``), a bare `` `%` `` to share the remaining chance
  equally, or a game-state query (`` `"Bob's Affection"%` ``) to weight an option by a value read at
  runtime. The report's AST tabs show each option's weight. See the
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
  [DocFX site](https://pengzhengyi.github.io/dialoguedown/) (a writer Guide, a
  Contributing section with the design notes, and a generated C# API reference) and a
  [live, read-only demo](https://pengzhengyi.github.io/dialoguedown/demo/) of
  the report, both published to GitHub Pages on every merge to `main`.
- **Development guardrails** — architecture tests that enforce the project's
  dependency direction (the engine-agnostic core stays free of the CLI, the
  visualization projects, and any engine/console packages), plus build-time size and
  complexity limits on the core library.
- **Project logo and favicon** — a chat-bubble Markdown "M" mark.
- Initial OSS community files and CI.

### Changed

- **A bare jump or a silent command is an effect, not speech** — a jump or a
  game-state command on its own line is now an effect-only *control line* with no
  speaker, so it is never attributed to a character or the configured default
  speaker; narration by the default speaker is unchanged. See the
  [Control Line](docs/contributing/design-notes/Control%20Line.md) note.
- **One maximize control for the whole report.** The visualization report's *full screen*
  toggle now lives once at the right end of the tab bar (with a matching exit control while
  maximized), instead of a separate button in each tab's toolbar; `f` and `Escape` still toggle it.
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

- **Nested lists in the Source preview keep an even vertical rhythm.** Items around a
  nested list are now spaced consistently instead of opening a large gap when a sub-list
  ends.
- **Nested bullet lists in the Source preview now vary their markers by depth.** The
  preview cycles disc → circle → square as unordered lists nest — matching a browser and
  VSCode's Markdown preview — instead of drawing a square at every level; numbered lists
  stay numbered.
- **The Source preview now continues soft-wrapped lines like VSCode.** A single line
  break joins into the same paragraph, and only an explicit hard break (two trailing
  spaces or a trailing backslash) starts a new line — matching CommonMark and the
  editor's own Markdown preview, instead of breaking on every newline.
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

[Unreleased]: https://github.com/pengzhengyi/dialoguedown/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/pengzhengyi/dialoguedown/releases/tag/v0.1.0

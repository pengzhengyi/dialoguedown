# Changelog

All notable changes to DialogueDown will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/), and this
project uses [Conventional Commits](https://www.conventionalcommits.org/) to keep
changes easy to categorize.

## [Unreleased]

### Added

- **A Playbook tab in the report** — after Dialogue Graph, the report now shows the compiled
  playbook itself: the JSON a game runtime loads, read-only and searchable, beside tables naming
  what a host must provide to run it, where a playthrough starts, and who can speak. Checking what
  a script actually ships as no longer means emitting a file and opening it elsewhere. Hovering a
  property explains it in the format's own words and washes in the stretch that description
  covers, the way an editor does for any `$schema`-linked JSON. It reads the way JSON does in an
  editor — a key, a string, a number, and a literal each on their own hue — and blocks fold from
  the gutter as they do in the Source and Config editors. Beside it, the playbook's header,
  speakers, and the anchors a jump may name each get a foldable, searchable table panel. A script
  with errors compiles no playbook, and the tab says so. See the
  [Playbook Tab](docs/contributing/design-notes/Playbook%20Tab.md) note.

- **The Dialogue Graph says what a jump was called** — a jump now reads as `⇒ through the gate`
  rather than `(jump)`, so one jump is told from another at a glance, and choosing its line shows
  the same words beside what the route means and the nodes it joins. A jump is kept nowhere else in
  that stage, so this is where its wording survives.

- **The playbook round-trip is asserted for generated scripts** — a playbook written by the
  compiler and read back by `PlaybookReader` now has to render identically, checked against
  hundreds of generated scripts rather than the four shipped examples the goldens pin. A playbook
  is the one way out of the compiler, so a field the reader silently dropped would reach a runtime
  as missing dialogue. See
  [How this project is tested](docs/contributing/testing.md#round-trip-tests).

- **Open a script without reloading the page** — clicking a script in the Explorer now replaces the
  report's contents instead of the page, so the reader keeps the window they were working in and
  the tab they had open; someone comparing two dialogue graphs is no longer sent back to Source on
  every click. Back and Forward move between scripts the same way, landing in View because Back is
  a navigation and not an intent to edit. Warm, a script opens in about 77 ms rather than 160 ms.
  Unsaved work is still settled first, and a script that compiles under a different `dialogue.toml`
  still loads a whole page. See the
  [Opening a Script Without Reloading the Page](docs/contributing/design-notes/Opening%20a%20Script%20Without%20Reloading%20the%20Page.md)
  note.

- **Fold ignored Markdown in the Source editor** — the editor's gutter now folds the same regions
  the Preview does, beside its own line-range folding, plus a pair of commands in its menu and keys
  (`Alt-i` / `Alt-o`). A `circle-slash` cue marks each ignored run without adding a second control
  to click. Folding is the editor's own, so the cursor cannot enter a folded range and hidden text
  is never silently changed. Source and Preview keep separate state: a reading choice must not hide
  the text a writer needs to edit. See the
  [Collapsing Across the Report](docs/contributing/design-notes/Collapsing%20Across%20the%20Report.md)
  note.

- **One way to fold** — the Source editor, the Preview, the Dialogue Graph, the legend, and the
  file Explorer now offer folding with the same chevron and the same two commands, instead of five
  renderings of the same idea. The Dialogue Graph also gains `Expand all` / `Collapse all` for
  scenes beside the legend group that lists them, stating the current view — including a mixed one
  — and re-framing the drawing. Each scene row shows its own state as a filled or hollow mark and
  says how many nodes it holds. See the
  [Collapsing Across the Report](docs/contributing/design-notes/Collapsing%20Across%20the%20Report.md)
  note.

- **Explorer on demand** — the project tree now starts out of the way when a script is
  open, and a Files control pinned at the tab bar's leading edge summons it and says whether
  it is showing. The empty shell still opens with the tree, where it is the only way forward,
  and an explicit choice is remembered. See the
  [Explorer Toggle](docs/contributing/design-notes/Live%20Visualization%20-%20Explorer%20Toggle.md)
  note.

- **Fold a scene in the Dialogue Graph** — a chevron on each scene band shuts the scene away to a
  single box that names it and counts what it holds, with the flow still passing through: routes
  crossing its border re-point at the box and everything downstream stays where it was. The band's
  own click still selects the scene, so inspecting one never rearranges the drawing. See the
  [Dialogue Graph — Region Fold](docs/contributing/design-notes/Dialogue%20Graph%20Region%20Fold.md)
  note.

- **Ignored-content Preview visibility** — a fixed footer matching Source's `#END` row reports how
  much Markdown the compiler left out, and `Expand all` / `Collapse all` set the view for the whole
  Preview. Each region also shows or hides on its own from its chevron, and the footer states a
  mixed view exactly. A hidden block keeps its kind and source-line count, a hidden inline link
  collapses to its host and still leads there, and Source stays visible and editable. See the
  [Ignored Markdown Preview Toggle](docs/contributing/design-notes/Ignored%20Markdown%20Preview%20Toggle.md)
  note.

- **Mermaid authoring diagrams** — fenced `mermaid` blocks render as
  theme-aware SVG diagrams in every Markdown preview while remaining outside the
  compiled dialogue. Invalid diagrams keep their source visible, and the
  self-contained report stays fully offline. See
  [Mermaid authoring diagrams](docs/contributing/design-notes/Mermaid%20Authoring%20Diagrams.md).
- **Configure unmodeled Markdown per project** — `dialogue.toml` can now choose
  `keep` or `ignore` for code blocks, thematic breaks, tables, raw HTML,
  autolinks, and other unmodeled constructs. Omitted kinds retain their built-in
  defaults, and invalid names or values report a located configuration error.
  See the [project configuration guide](docs/guide/configuration.md#unmodeled-markdown).
- **Dialogue Graph tab** — the report now shows the compiled flow as a fifth stage: every block
  as a node, joined by the edges that lead between them, so you can see where a choice goes,
  which scene a jump enters, and — because the tab shows every node rather than only the ones a
  walk reaches — which lines nothing reaches at all.

  Each kind of route is drawn as itself: a succession is a plain arrow, a jump a long dash, a
  choice arm a fine dotted line, a conditional branch a dashed one, and a line nothing reaches
  is barred with crosses. Every route names itself on hover, carries a pointer that says what it
  does, and can be clicked to read what that kind of route means and which two nodes it joins.
  A scene is drawn as a tinted band around the nodes written under it — clickable in turn, to
  see how much it holds, what crosses its border, and the text it was written as. Selecting a
  node lists what leads to it and where it leads, and every row walks the flow. See the
  [Dialogue Graph Visualization Tab](docs/contributing/design-notes/Dialogue%20Graph%20Visualization%20Tab.md) note.
- **Dialogue graph — the compiler's final stage** — a clean compile now lowers its semantic
  model into an immutable **dialogue graph**: one node per block (a line, a control line, a
  choice, a random choice, a block conditional, and the terminal End) joined by typed edges
  (fall-through, jump, choice option, random option, and conditional branch). Guards ride the
  edge when they withhold a route and the node when they withhold a block's content, every
  node carries the source it came from, and scenes are overlaid as named regions a jump can
  enter. This is the artifact a future runtime walks to play a script — playing it is still to
  come ([#45](https://github.com/pengzhengyi/dialoguedown/issues/45)). See the
  [Dialogue Graph](docs/contributing/design-notes/Dialogue%20Graph.md) note.
- **`ddown` shows usage examples** — the `compile` and `visualize` commands now list example
  invocations in their `--help` output.
- **Example scripts and a live demo gallery** — three genre examples (a high-rise fire-safety
  drill, an RPG quest, and a visual novel) plus a deliberately-broken diagnostics tour, published
  as a multi-example gallery on the docs demo site beside the original walkthrough script.
- **Problems panel** — every diagnostic the compiler reports, listed in a footer drawer, with
  each row jumping to the text it describes. The status line now carries error, warning, and
  info counts that open it, so problems are visible from every tab instead of only as squiggles
  inside the Source editor. Press `p` to open it. See the
  [Problems Panel](docs/contributing/design-notes/Live%20Visualization%20-%20Problems%20Panel.md) note.
- **Jump from the source to a stage** — right-click a selection in the Source editor (or press
  `Alt-J`) and choose **Jump to ▸** a compiler stage to open that tab with the enclosing node
  revealed and centered. It is the reverse of **Jump to source**, and works in View and Edit. See
  [Reverse Jump](docs/contributing/design-notes/Live%20Visualization%20-%20Reverse%20Jump.md).
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

- **`--emit` moved from `visualize` to `compile`** — writing the compiler's stage graphs as
  Graphviz DOT never opened a browser, so it belonged with the command that checks and exports a
  script rather than the one that previews it. Use `ddown compile <script> --emit dot -o
  stages.dot`; `compile` gains `-o`/`--output` to receive it. The old form fails with a message
  pointing at the new one rather than being ignored, so a script that used
  `visualize --emit dot -o out.dot` cannot silently receive an HTML report instead.

- **Property tests guard the invariants examples cannot reach** — generated scripts now assert
  that every AST node's span addresses text that exists, that a child's span sits within its
  parent's, and that compiling never throws. See
  [CONTRIBUTING](CONTRIBUTING.md#properties-beside-examples).

- **CI gates branch coverage too** — a decision point can be fully line-covered with only one of
  its paths ever taken, so the build now fails below **85% branch** as well as 90% line. The check
  reads ReportGenerator's own summary rather than raw Cobertura, so line, branch, and method rates
  come from one already-merged source.

- **Switching scripts in the served report is more than twice as fast** — the server now watches a
  folder once, instead of starting a fresh file-system watch for every script opened. Opening a
  script falls from about 330 ms to about 135 ms, and the switch behind it from about 150 ms to
  around a millisecond once its folder is known. Hot reload is unchanged. See the
  [One Watcher for the Served Tree](docs/contributing/design-notes/One%20Watcher%20for%20the%20Served%20Tree.md)
  note.

- **One home per concept in the documentation** — a committed duplication scan
  (`.github/scripts/find-doc-duplication.py`) compares word shingles across the docs
  tree, so a concept paraphrased in two places is caught rather than left to drift.
  Thirteen duplicated passages became two intended ones; `AGENTS.md` now carries only
  what an agent needs before it can act and links to
  `.github/copilot-instructions.md` for the rest. The scan is a release gate.

- **The report loads about three times less to open a script** — Mermaid is no longer part of the
  client every reader downloads. A served report fetches it the first time a script shows a
  diagram, and an exported report carries it only when that script draws one, so a typical export
  falls from 4.8 MB to 1.4 MB. Diagrams render exactly as before, still with no CDN and still
  offline. See the
  [Development Cycle Optimization](docs/contributing/design-notes/Development%20Cycle%20Optimization.md)
  note.

- **Guardrails against the mistakes a compiler cannot afford** — the core may no longer read the
  clock, mint a `Guid`, or draw randomness, so a script always lowers to the same graph, and the
  Dialogue AST is held immutable so no later stage can change what an earlier one produced.

- **Opening a script in the served report is about four times faster** — the served page now links
  the client from a content-addressed URL instead of carrying a copy of it, so a browser downloads
  and compiles the client once and reuses it for every script it opens. Measured click-to-report on
  an unchanged script: about 1.3 s down to about 0.3 s, and 1.86 MB down to 4.7 kB per navigation.
  An exported report is unaffected — it still inlines everything, because a file that leaves the
  server has to work offline. See the
  [Development Cycle Optimization](docs/contributing/design-notes/Development%20Cycle%20Optimization.md)
  note.

- **Every assembly's root namespace now names its parts** — an architecture rule caps how many
  types an assembly's root namespace may hold, so a layer cannot flatten into an unnamed list.
  Satisfying it moved the bulk of `DialogueDown.Visualization`,
  `DialogueDown.Visualization.Live`, and `DialogueDown.ConfigurationLoader` into sub-namespaces
  that name their roles. See the
  [Namespace Layout](docs/contributing/design-notes/Namespace%20Layout.md) note.

  **BREAKING CHANGE:** consumers of those three assemblies need a `using` directive for the new
  sub-namespace. Nothing was renamed or removed, so each type is found under its role —
  for example `DialogueDown.Visualization.Display` for `DisplayGraph`,
  `DialogueDown.Visualization.Live.Serving` for `LiveSession`, and
  `DialogueDown.ConfigurationLoader.Errors` for `DialogueConfigurationException`.

- **Full-suite test commands state how many tests they expect** — every documented
  `dotnet test` command now passes `--minimum-expected-tests`, so a run that executes far
  fewer tests than the suite holds fails loudly instead of reporting a green
  "Zero tests ran". Inner-loop runs stop at the first failure, and a new `test: class`
  VS Code task selects one class through xUnit's own class filter. See the
  [Development Cycle Optimization](docs/contributing/design-notes/Development%20Cycle%20Optimization.md)
  note for the measurements behind rejecting the platform's speed options.

- **.NET 10 LTS, without moving Godot projects with it** — the libraries a game references
  (`DialogueDown`, `DialogueDown.ConfigurationLoader`) now ship for both `net8.0` and
  `net10.0`, so a Godot project keeps the runtime Godot bundles while the toolchain moves to
  LTS ahead of .NET 8's end of support on November 10, 2026. The `ddown` CLI now targets
  `net10.0` and requires the .NET 10 runtime; building the repository now needs the .NET 10
  SDK. See the
  [Target Frameworks](docs/contributing/design-notes/Target%20Frameworks.md) note.

- **Breaking (pre-1.0): configuration values are deeply immutable and compare by
  content.** `CompilerOptions.Speakers` and configured-speaker tag lists are now
  `ImmutableArray<T>`; `CompilerOptions.UnmodeledMarkdown` is now an
  `ImmutableDictionary<TKey, TValue>`. Equivalent options compare equal
  regardless of map insertion order, and snapshot constructors accept mutable
  sequences safely. Callers assigning an existing mutable list or dictionary
  directly to a property must use a collection expression, convert it to the
  matching immutable type, or use the snapshot constructor. See
  [Configuration](docs/contributing/design-notes/Configuration.md).
- **The graph legend now draws each route as the line it really is** — the same dashes the canvas
  draws, ending in the same arrowhead, and stamped with the same crosses where a line marks a node
  nothing reaches. A jump is drawn dash‑dot so it differs from a conditional in kind rather than
  only in dash length, and the legend row reads "Conditional".
- **The report's tables now run on `table-core` v9.** Sorting, searching, and faceted filtering
  behave exactly as before; the upgrade keeps the report on a supported major of its headless
  table engine.
- **Tighter report layout.** The visualization report trims the chrome so more of
  the window goes to content: a more compact header, the active-tab underline sits
  under its label, the main area runs edge to edge, and the Source tab's preview
  drops its redundant frame.
- **A compile now succeeds or fails, and says which.** `Compile` returns a
  `CompilationSuccess` carrying every stage artifact, or a `CompilationFailure` carrying how
  far it got — instead of one result whose later artifacts might be missing. A script with an
  error is a failure and produces no graph, since a model the compiler had to recover no
  longer describes what you wrote; everything the compile did reach is still there, so the
  report keeps showing a broken script's stages. `Source`, `HasErrors`, and
  `LocatedDiagnostics` are unchanged. See the
  [Compilation Outcome](docs/contributing/design-notes/Compilation%20Outcome.md) note.
- **A jump to another file now warns instead of passing silently** — a target naming a file or
  a URL (`=> [Meet Bob](chapter-02.md#meet-bob)`) reports **DLG2016** and leads nowhere, so
  reading continues with the next line. Cross-file targets are not resolved yet
  ([#59](https://github.com/pengzhengyi/dialoguedown/issues/59)); until they are, the warning
  says so rather than leaving a jump that quietly goes nowhere.
- **Core quality guardrails now fail the build.** The engine-agnostic core
  (`src/DialogueDown`) enforces its size and complexity limits as errors rather
  than warnings, forbids mutable global state (`CA2211`), and caps public methods
  per type via an architecture test. The CLI and visualization projects stay
  exempt. See [Contributing](CONTRIBUTING.md#core-quality-guardrails).
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
  `visualize` now opens the report shell directly on an **empty state** — the
  Explorer over your project beside a "create your first dialogue file" call to action — and
  `visualize <script>` opens that same shell on your script, so the Explorer sidebar is available
  whichever way you start and there is a single page to learn. Serving a script that links images
  above its folder still resolves them with your consent (or an explicit `--root`). See the
  [Unified Served Shell](docs/contributing/design-notes/Live%20Visualization%20-%20Unified%20Served%20Shell.md) note.

### Removed

- **Compiler-stage Mermaid emission** — `ddown visualize --emit mermaid`,
  `EmitFormat.Mermaid`, and the C# `MermaidRenderer` have been removed. Use
  `--emit dot` for compiler graphs; fenced Mermaid blocks now render in the HTML
  report.

### Fixed

- **The live end-to-end suite no longer flakes on a busy machine, or tests the wrong build** —
  opening a script compiles it on the server, which outgrew the assertion's default timeout under
  load. The suite now waits long enough for the work it drives, and refuses to start when a
  fixture port is held by a server serving another checkout's fixtures instead of reusing it in
  silence. See [the `visualize` CLI and live server](CONTRIBUTING.md#the-visualize-cli-and-live-server).

- **Jumping from the Source into the Dialogue Graph lands on the right node** — a selection
  resolved to whatever the flow led to rather than what held it, so the graph revealed the wrong
  node and previewed a region that ran past the end of the scene. Choosing **Jump to ▸ Dialogue
  Graph** now reveals the node the text belongs to, matching what the other stages resolve to, and
  every stage brings the revealed node up to a readable scale rather than centering it at a
  whole-script fit where it is a few pixels tall.

- **Go to line is usable in every editor** — jumping to a line already worked in the Source,
  Config, and Playbook editors, but only from a shortcut nothing mentioned, and its dialog arrived
  in browser-default chrome with an unreadable button, pushing the document up from the editor's
  foot. It now opens on <kbd>Ctrl-G</kbd> as it does in VS Code, and floats over the text near the
  top — focused on arrival, dismissed by Escape or by clicking away, gone once the jump is made,
  nothing reflowing behind it. A line reading under the field says what Enter will do, and teaches
  the expression as it goes: it names the range while the field is empty, and offers the column
  once a line is entered. A line number, `line:column`, a `+`/`-` offset, or a `%` of the document
  all work, and none of them were discoverable before.

- **A second script no longer silences the first one's tab** — opening a script stopped the one
  already open from hot-reloading in another browser tab, with nothing to say it had: the tab kept
  its connection and simply stopped being told anything. A tab now names the document it is
  showing when it subscribes, so it is told that its script stopped being served rather than
  waiting on a stream that will never speak again.

- **The demo page advertises the Dialogue Graph stage** — the report has rendered it since the
  graph stage landed, but the landing page's hand-written stage list never gained it. Two release
  checks that used to be shell scripts run over rendered Graphviz text are now tests, so the page
  and the example corpus are checked on every run instead of once at release: one compares the
  advertised stages against the ones the visualization projects, the other asserts every construct
  the compiler models appears in a shipped example.

- **The report no longer shows a "Back to the launcher" arrow that goes nowhere** — every served
  report now lives under the `/r/` mount, so the check that once meant "opened through the
  launcher" had come to match every report, while the destination redirected straight back to the
  report you were on. The launcher it named is gone: the Explorer sidebar and its **Files** control
  reach the project's scripts without leaving the page, and offer everything that landing did.

- **Transitive NuGet packages now reach the dependency graph** — `global.json` pinned the SDK
  without its feature band (`10.0.0`), and `actions/setup-dotnet` rejects a short version whenever
  `rollForward` is set. GitHub's automatic dependency submission reads that file, so every run
  failed and the graph held only the directly declared packages; an indirect dependency missing
  from the graph raises no Dependabot alert. The pin is now a full SDK version (`10.0.100`), which
  resolves to the same SDK as before.

- **Undo no longer reaches into a script you left** — replacing the editor's buffer kept the
  previous document undoable, so one undo after opening another file could pull that file's text
  into this buffer, and the next save would have written it to the wrong path. Opening a document
  now clears the history; a reload or a discard, which revert the same file, still keep it.

- **The Source editor's `#slug` chip survives a late parse** — the chip on the active heading line
  could stay hidden when a script opened on a busy machine, and only appear once the caret moved.
  The editor's first syntax parse is time-bounded, and the hint ignored the update that publishes
  the finished parse. It now refreshes on that update too, so the chip appears as soon as the
  heading is recognized.

- **A jump from ignored Markdown says where it landed** — the compiler makes no node from text
  it leaves out, so **Jump to** settles on the node around it. A note now appears beside that node
  on arrival, rather than presenting it as what the text became. The **Markdown AST** tab's
  description no longer reads as a complete parse of the file, so a construct missing from that
  tree is expected rather than surprising.

- **The Source tab's bottom row reads as one bar, edge to edge** — the split divider no longer
  shows through as a white notch between the `#END` footer and the ignored-content footer, and the
  main area is full-bleed, so nothing insets the bar from either window edge. The Explorer, the
  Semantic Model's cards now carry their own spacing rather than borrowing it from that inset, and
  a legend is headed by its name beside the control that folds it, instead of opening under a
  button sitting alone in the corner.

- **Front matter now reads as YAML metadata in the Source editor** — a canonical leading
  `--- … ---` block receives YAML keys, values, comments, indentation, and folding instead of
  being misread as ordinary Markdown; the dialogue body keeps its existing Markdown and
  compiler-projected highlighting. See
  [Front Matter Source Highlighting](docs/contributing/design-notes/Front%20Matter%20Source%20Highlighting.md).
- **Co-located diagnostics now present deterministically** — overlapping editor
  marks use the severest active level while hover and the Problems panel retain
  every message; diagnostics at one position list errors before warnings and
  infos regardless of compiler/LSP array order.
- **Jump indicators stay with their targets in previews** — at narrow widths, `=>` now wraps with
  the linked target instead of being left alone on the previous line.

- **The editor shows what becomes of a script's Markdown** — the constructs the handling policy
  leaves out are dimmed and enclosed in eye-marked Preview regions instead of reading like
  dialogue, the blockquotes that carry control blocks are no longer muted and carry a question
  sticker, and comments read as the writer-only notes they are. See
  the [Unmodeled Markdown Highlighting](docs/contributing/design-notes/Unmodeled%20Markdown%20Highlighting.md)
  note.

- **Cross-links in the Dialogue Graph are far enough apart to aim between.** Node labels are now
  clipped to a measured width rather than a count of characters — thirty `W`s are more than twice
  thirty `i`s, so the gap the routes climb in was unknown and could not be widened safely. With the
  gap a known width, routes climbing in one column each take a corridor of their own, 18 units
  apart instead of 7.
- **A stage opens on what it draws.** Every graph tab used to open at full size anchored on its
  root, which on a long script showed a handful of nodes and left the reader to hunt for the rest.
  A stage now frames the whole of itself, clear of the legend — and where the whole of it will not
  fit legibly, it opens at the start of it rather than shrinking to an unreadable smudge. A zoom
  chosen on another tab is still inherited exactly.
- **The legend folds away.** It floats over the drawing it describes and has grown — nodes, edges,
  and now regions — so a reader who has learned it can fold it down to a single button and have
  the canvas back.

- **The Dialogue Graph tab now renders.** It shipped unable to draw anything: the client lays
  every stage out as a tree, and a dialogue graph is not one, so any document with a cycle
  reported `Failed to render stage: cycle`. The projection now names one parent per node and
  draws the remaining edges as cross-links.
- **Lines no longer strike through the words they belong to.** A node writes its label to the
  right of its dot, so an edge leaving from the dot ran through its own text, and a long
  cross-link lay across every row it passed. Text is now measured rather than estimated, and a
  cross-link travels a lane below the drawing.
- **A list in a node preview renders as a list.** The framework's reset made every `li` a plain
  block, and a marker is only drawn for a list-item box — so a choice's arms read as loose
  paragraphs. This affected every preview in the report, not only the graph tab.
- **Dimming a legend row shows immediately.** The row's hover preview held the category at full
  strength under a resting pointer, so the click looked like it had done nothing.
- **Selecting a node by name reveals it.** A search hit or a neighbor row inside a collapsed
  branch was marked but never shown; the fold over it now opens.
- **A region's border names both of its ends** — Source, Edge, Destination — because a scene
  entered at its first line and one entered halfway are different stories, and only both ends tell
  them apart. Regions also take their own group in the legend, with the tint their band is drawn
  with and how much each holds, grouped under the kind of grouping they are — a fold that already
  has a shelf for the next kind.
- **A graph's nodes no longer fold.** A tree's children are its content, so hiding them hides only
  detail; a graph's are an accident of which route happened to reach them first, and folding one
  took away nodes other routes still lead to, along with the edges into them.
- **A graph frames itself when you arrive at it.** An untouched tab inherited the whole camera,
  pan included — and a pan means something only against the graph it was made on. The dialogue
  graph runs far wider than the trees beside it, so arriving from a panned tab left the reader
  looking at empty canvas. Only the zoom travels between graphs now.
- **Routes ending at one node can be told apart.** Every jump into a scene lands on its entry, and
  all of them climbed in that node's own column — one line to the eye, and a coin toss to the
  pointer. Each now climbs in a corridor of its own and leans in from its own row, and the pointer
  resolves to the line nearest it rather than to whichever target happens to be on top.

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

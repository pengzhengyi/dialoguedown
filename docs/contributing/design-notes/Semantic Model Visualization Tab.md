# Semantic Model Visualization Tab

> [!NOTE]
> Status: **implemented** (an enhancement to
> [Compilation Visualization](./Compilation%20Visualization.md), showing the
> [Semantic Analyzer](./Semantic%20Analyzer.md)'s output). Unlike the AST stages, the
> semantic analyzer produces **several outputs, not one graph**: a scene tree plus a
> speaker table, an anchor table, and a jump-resolution table. So its tab is an
> **analytics layout** — the **scene tree** as the graph on the left, and the three
> tables stacked as **collapsible panels** down the right — with **cross-linking**, so
> hovering a scene or speaker in one place highlights it everywhere it appears.
>
> Like the rest of the visualization tooling, this surface is "vibe-coded" (see the
> visualization note's maturity caveat); the core engine stays the reviewed surface.

## Table of contents

- [Goal & scope](#goal--scope)
- [Ubiquitous language](#ubiquitous-language)
- [Functionality checklist](#functionality-checklist)
- [Design](#design)
  - [Payload shape](#payload-shape)
  - [Cross-linking by entity key](#cross-linking-by-entity-key)
  - [Layout](#layout)
  - [Flow](#flow)
- [Interfaces & abstractions](#interfaces--abstractions)
- [The three tables](#the-three-tables)
- [Key design decisions](#key-design-decisions)
- [Error & boundary cases](#error--boundary-cases)
- [Integration](#integration)
- [Testability](#testability)
- [Deferred enhancements](#deferred-enhancements)
- [Implementation checklist](#implementation-checklist)

## Goal & scope

The report shows the Markdown, Dialogue, and Desugared ASTs — each a single graph in
its own tab. The **semantic analyzer** runs after desugar and exposes a
[`SemanticModel`](./Semantic%20Analyzer.md) on the compilation result; this tab shows
it.

The semantic model is **not one graph**. It is a **scene tree** (the nested scenes)
plus three lookup tables the analysis resolved — **speakers**, **anchors**, and **jump
resolutions**. A single graph tab cannot represent that well. This component adds a
**Semantic** tab with an **analytics layout**:

- the **scene tree** as the graph in the main (left) area, reusing the existing
  interactive tree view — each scene expands to the **script blocks** it owns; and
- the **speaker table**, **anchor table**, and **jump-resolution table** as
  **collapsible table panels** stacked down the right column, which as a whole is
  resizable and can be hidden to give the graph full width.

The tab is **cross-linked**: hovering a scene, a speaker, or a jump anywhere — a table, a
scene node, or a script block in the tree — highlights that same entity everywhere it
appears, so a reader can see which scene a jump resolves to, or every line a speaker speaks.

**In scope:** the C# projection of the semantic model into a scene-tree graph (with each
scene's script blocks) plus the three tables, all sharing cross-link keys; the TS
analytics-layout tab (graph + resizable, collapsible stacked tables) with cross-link
highlighting; and wiring the tab through the existing report payload. **Out of scope:**
changing the analyzer or its model; the flow graph (succession/choice/jump *edges*, a later
component); editing from this tab; and speaker-driven autocomplete (the next component,
tracked as [#71](https://github.com/pengzhengyi/dialoguedown/issues/71)).

## Ubiquitous language

Reuses the [Semantic Analyzer](./Semantic%20Analyzer.md)'s terms so one concept keeps
one name across the analyzer, this tab, and the code.

| Term | Meaning |
| --- | --- |
| **Scene tree** | The nested scenes and, under each, the script blocks it owns — the tab's graph. A scene's node links to its anchor-table row. |
| **Script block** | A piece of a scene's content (a line, speaker, choice, jump, tag, …), described exactly as on the Desugared AST tab. |
| **Speaker table** | Rows of resolved speakers (name, `@id`, tags, whether default). |
| **Anchor table** | Rows mapping a scene's `#slug` anchor to its scene. |
| **Jump-resolution table** | Rows of each jump and what it resolved to (a scene, a deferred file target, or unresolved). |
| **Entity** | A cross-referenced thing with a stable identity: a **scene** or a **speaker**. Cross-linking highlights an entity everywhere it appears. |
| **Entity key** | The stable string identifying an entity across the graph and tables, e.g. `scene:the-market`, `speaker:@guide`. A scene node or table row *is* an entity (its `entityKey`); a jump/speaker block node or table cell *references* one (its `refKey`). |
| **Table panel** | One collapsible container in the right column holding a table; collapses to a labeled horizontal bar. |

## Functionality checklist

- [x] A **Semantic** tab appears after the Desugared AST tab.
- [x] Its main area shows the **scene tree** as an interactive graph (zoom, pan, fold,
      full screen, position memory) — reusing the existing tree view.
- [x] Each scene expands to the **script blocks** it owns (lines, speakers, choices, jumps,
      …), described exactly as on the Desugared AST tab, expandable and collapsible per node.
- [x] The **scene backbone is emphasized** — scenes and the root render as larger,
      thicker-ringed circles joined by bolder edges, so the tab reads as a scene tree with
      content hanging off it rather than a flat node tree.
- [x] The right column stacks three **table panels**: speaker, anchor, jump resolution.
- [x] The whole tables column is **resizable** (drag the divider) and **collapsible** (a
      toggle hides it so the graph fills the width); both choices persist across reloads.
- [x] Each table panel **collapses to a horizontal bar** (title + row count) and expands
      again; the choice persists across reloads.
- [x] **Cross-linking:** hovering a scene (row/cell or graph node), a **speaker mention** in
      the tree, or a **jump** highlights the same entity everywhere — the scene in the graph,
      the anchor table, and its jumps; a speaker across its tree mentions and its speaker row.
- [x] The jump-resolution table shows each jump's **resolution kind** — a scene link, a
      deferred file target, or unresolved — with the scene link cross-referencing the
      scene entity.
- [x] The speaker table marks the **default** speaker and lists each speaker's **tags**.
- [x] Empty tables render a clear "none" state, not a blank panel.
- [x] Clicking a scene or block shows its attributes, source, and a rendered preview in a
      **node-details panel** pinned to the top of the tables column (sticky); it auto-expands
      on selection.
- [x] The tab is read-only on every mode; the View/Edit toggle stays frozen (as on the
      other graph tabs).

## Design

### Payload shape

The report payload today is `{ mode, path, source, stages }`, where each `stage` is a
graph (`title`, `description`, `nodes`, `edges`). The semantic stage **is still a graph**
— the scene tree — so it reuses that, and adds its tables alongside:

- Extend `Stage` with an optional **`tables?: SemanticTable[]`**. A stage with no
  `tables` renders exactly as today (a plain graph tab). A stage with `tables` renders in
  the analytics layout.

This keeps one uniform tab model: the scene tree flows through the entire existing tree
view (camera memory, fold, full screen, cross-stage colors) for free, and the tables are
purely additive. (Alternative — a separate top-level `semantic` payload with a bespoke
tab — was rejected: it would duplicate the tree view and split the tab model.)

### Cross-linking by entity key

Cross-linking needs a **shared identity** for each entity that appears in more than one
place. The projection emits **entity keys**:

- A scene-tree **graph node** for a scene carries `entityKey = scene:<anchor>`.
- An **anchor-table** row carries `entityKey = scene:<anchor>` (the row *is* that scene).
- A **jump-resolution** row that resolved to a scene carries, on its target cell,
  `refKey = scene:<anchor>` (it *references* that scene).
- A **speaker-table** row carries `entityKey = speaker:<id-or-name>`.

The TS side builds one index from `entityKey`/`refKey` → the DOM elements carrying it.
Hovering any element with a key adds a `highlight` class to **every** element sharing that
key, across the graph and all tables; leaving clears it. No positional or title-matching
guesswork — the key is the single source of truth, mirroring how the model itself keys
scenes by anchor and speakers by name/id.

### Layout

```text
┌──────────────────────────────┬───────────────────────────┐
│                              │ ▸ Speakers (3)            │  ← collapsed bar
│        Scene tree            ├───────────────────────────┤
│        (graph: zoom,         │ ▾ Anchors (4)             │
│         pan, fold,           │   #the-market → The Market│  ← expanded table
│         full screen)         │   #the-forest → The Forest│
│                              ├───────────────────────────┤
│                              │ ▾ Jumps (2)               │
│                              │   [east] => The Market    │
└──────────────────────────────┴───────────────────────────┘
```

The right column reuses the **collapsible-panel** pattern already in the report (the
node-inspector collapse from the side-panels work): each table panel has a header bar with
a collapse toggle; collapsed, only the bar shows. The three panels stack and scroll
independently. For this tab, the tables **replace** the single node-detail inspector.

### Flow

```mermaid
flowchart LR
  model["SemanticModel<br/>(compilation result)"] --> proj["SemanticProjection (C#)"]
  proj --> tree["Scene-tree graph<br/>(nodes + edges, entity keys)"]
  proj --> tables["3 tables<br/>(rows + entity/ref keys)"]
  tree --> stage["Stage { nodes, edges, tables }"]
  tables --> stage
  stage --> json["report JSON"]
  json --> view["createSemanticView (TS)"]
  view --> graph["tree view (scene tree)"]
  view --> panels["stacked collapsible table panels"]
  graph --> link["entity-key highlight index"]
  panels --> link
```

## Interfaces & abstractions

| Type / function | Responsibility | Collaborators |
| --- | --- | --- |
| `SemanticProjection` (C#) | Project a `SemanticModel` (and the source text) into a scene-tree `DisplayGraph` plus the three `SemanticTable`s, all sharing cross-link keys. | `SceneTreeProjection`, `GraphWalk` |
| `SceneTreeProjection : INodeProjection<object>` (C#) | Describe/'`Neighbors`' a scene (its blocks then its subscenes) and delegate each script block to the shared `DialogueAstProjection`, adding a `RefKey` on a speaker mention or a scene-resolving jump. | `GraphWalk`, `DialogueAstProjection`, `SpeakerTable`, `JumpResolutionTable` |
| `NodeDescription.TypeName` / `DisplayNode.TypeName` (C#) | Optional legend name for a node whose label is content (a scene title) rather than a type; the legend groups by it when present. | `GraphWalk`, `createLegend` |
| `NodeDescription.RefKey` / `DisplayNode.RefKey` (C#) | Optional cross-link key when a node *references* an entity — a jump's target scene, a speaker mention — the symmetric partner of `EntityKey`. | `GraphWalk`, `createEntityHighlighter` |
| `SemanticTable` / `SemanticRow` / `SemanticCell` (C#) | A serializable table: title, columns, and rows of cells; a cell may carry `entityKey`/`refKey`. | `DisplayGraphJson` |
| `Stage.tables` (TS + C# payload) | Optional tables riding alongside a stage's graph; absent ⇒ a plain graph tab. | `addStageTab`, `DisplayGraphJson` |
| `createSemanticView` (TS) | Build the analytics tab: the scene-tree tree view + stacked table panels, wired for cross-link highlight, with a draggable divider that resizes and hides the tables column. | `createTreeView`, `createTablePanel`, `createEntityHighlighter`, `initCollapsiblePanel` |
| `createTablePanel` (TS) | Render one `SemanticTable` as a collapsible panel (header bar + table) carrying the cross-link keys. | `initCollapsiblePanel` |
| `createNodeDetailPanel` (TS) | The sticky node-details panel: a collapsible panel pinned to the top of the tables column that shows a clicked node's attributes, source, and preview (auto-expanding on select). | `initCollapsiblePanel`, `nodeDetailTitle`/`nodeDetailBody` |
| `nodeDetailTitle` / `nodeDetailBody` (TS) | Shared node-detail rendering (category dot + label; attributes + source + preview), reused by both the standard `#detail` inspector and the sticky panel. | `renderMarkdown` |
| `createEntityHighlighter` (TS) | Index elements by `entityKey`/`refKey` and toggle the shared `entity-highlight` class on hover. | — |

## The three tables

| Table | One row per | Columns | Entity / ref keys |
| --- | --- | --- | --- |
| **Speakers** | resolved speaker | Name, `@id`, Tags, Default | row `entityKey = speaker:<id-or-name>` |
| **Anchors** | scene with an anchor | Anchor (`#slug`), Scene (heading text), Level | row `entityKey = scene:<anchor>` |
| **Jump resolutions** | analyzed jump | Jump (label), Target (`#slug`), Resolves to (scene / file — deferred / unresolved) | "Resolves to" cell `refKey = scene:<anchor>` for a scene jump |

## Key design decisions

- **The scene tree is the stage's graph; tables ride in `Stage.tables`.** Reuses the whole
  tree view and keeps one tab model; a table-less stage is unchanged. (See
  [Payload shape](#payload-shape).)
- **Each scene expands to its script blocks, reusing the Desugared AST projection.**
  `SceneTreeProjection` is a composite over `object`: a scene yields its blocks then its
  subscenes, and every block is described by the shared `DialogueAstProjection`, so the tree
  reads identically to the Desugared AST tab without duplicating that logic.
- **Cross-link by entity key, not by matching.** The projection emits `scene:<anchor>` and
  `speaker:<id-or-name>` keys; the UI highlights by exact key. A scene node or table row
  *is* an entity (`entityKey`); a jump/speaker block node or table cell *references* one
  (`refKey`) — the highlighter treats the two symmetrically. This mirrors the model's own
  keying and avoids brittle title/position matching. (See
  [Cross-linking](#cross-linking-by-entity-key).)
- **Reuse the collapsible-panel pattern for both the table stack and the whole column.**
  Each table panel and the column as a whole reuse the report's collapse toggle (with
  persistence); a pointer-captured divider resizes the column, mirroring the Source tab's
  editor/preview split rather than inventing a new affordance.
- **A dedicated `SemanticProjection`, exposed through the existing friend seam.** The model
  is `internal`; the visualization project already has friend access via
  `CompilationResult.Semantics`, so the projection lives on the visualization side with no
  new public surface. The scene-tree graph goes through `GraphWalk` like every other stage.
- **Hover-to-highlight for v1.** The user's spec is hover-driven; a sticky click-to-pin
  selection is a possible later enhancement (see
  [Deferred enhancements](#deferred-enhancements)).
- **A node's legend name can differ from its label.** A scene node labels itself by its
  **title** (so the tree reads as scenes), which would make the legend list every title.
  So a node carries an optional `TypeName` ("Scene", "Document") that the legend groups and
  labels by, falling back to the label for AST stages that label themselves by type.
- **Emphasize the scene backbone.** The tree view tags scene/root nodes (those with a
  `typeName`) and the edges into scenes with a `scene` class and draws scene circles larger;
  CSS then bolds their ring and edges. Only the semantic tab sets `typeName`, so the shared
  tree view leaves every other tab unchanged.

## Error & boundary cases

| Case | Behavior |
| --- | --- |
| No scenes (flat document, root only) | The scene tree shows just the root; the anchor table shows its "No scenes." note. |
| No speakers beyond the default | The speaker table shows the default speaker row only. |
| No jumps | The jump-resolution table shows its "No jumps." note. |
| Jump resolved to a **file target** (deferred) | Row shows the file/anchor as text with a "deferred" note; **no** scene `refKey`. |
| **Unresolved** jump (empty target) | Row shows "unresolved"; no `refKey`. |
| A scene whose heading text is empty after slugging | Cannot happen — the analyzer rejects empty-slug headings upstream; the projection assumes a valid model. |
| Duplicate-looking headings | The analyzer already made anchors unique (or threw); the projection trusts the model's keys. |

## Integration

- **C#:** `CompilationVisualizer.BuildStages` appends the semantic stage after the three
  AST stages, projecting `result.Semantics` through `SemanticProjection`. `DisplayGraphJson`
  serializes the new `tables` field (omitted when null, like other optional payload fields).
- **TS:** `runApp`'s `addStageTab` routes a stage that has `tables` to `createSemanticView`
  instead of the plain graph path; the scene tree still uses `createTreeView`, so camera
  memory, fold, and full screen work unchanged. The tab freezes the View/Edit toggle like
  the other graph tabs and hides the shared node-detail inspector; clicking a node instead
  fills its own **node-details panel**, pinned (sticky) to the top of the tables column and
  reusing the shared detail rendering.
- The report stays a single self-contained offline file; no new runtime dependency.

> [!NOTE]
> **Rendering:** the scene-tree SVG must sit in an **out-of-flow, contained paint
> context** — `.semantic-graph svg.tree` is `position: absolute; inset: 0` and the column
> uses `contain: layout paint`, mirroring how every other stage graph lives in an absolute
> `section.stage`. As an in-flow flex item beside the scrollable tables, Safari's GPU
> compositor left **ghost copies of the tree while zooming** (the previous frame's layer was
> not fully invalidated). Headless browsers use software rendering and cannot reproduce it,
> so verify zoom in real Safari when touching this layout.

## Testability

- **C# projection** — unit-test `SemanticProjection` against a compiled sample: assert the
  scene-tree graph's nodes/edges and entity keys, and each table's rows/cells and
  entity/ref keys. `SceneTreeProjection` is tested through `GraphWalk` like the AST
  projections.
- **TS** — unit-test `createEntityHighlighter` (hovering a key highlights all elements
  sharing it) and the table/panel rendering (jsdom). End-to-end (Playwright): the Semantic
  tab shows the graph + three collapsible tables; hovering an anchor row highlights the
  matching scene node and jump row; a table collapses to its bar and reopens.
- Mirror the one-file-per-source layout; high, meaningful coverage as elsewhere.

## Deferred enhancements

Settled for the current build, with these enhancements deliberately deferred:

1. **Click-to-pin selection.** Cross-linking is **hover-to-highlight** only. A sticky
   click-to-pin selection (so a reader can move the mouse away while comparing) is a
   possible fast follow.
2. **Scene-to-speaker aggregation.** A speaker cross-links from each of its **mentions** in
   the tree and its speaker row. Tagging each *scene* node with the speakers that appear in
   it (so hovering a speaker lights up whole scenes, not just the mentions) is deferred — it
   needs the projection to aggregate speakers per scene.

## Implementation checklist

- [x] C#: `SemanticTable`/`SemanticRow`/`SemanticCell` model + `SceneTreeProjection` +
      `SemanticProjection`; unit tests.
- [x] C#: `Stage.tables` in the payload; `DisplayGraphJson` serializes it; `BuildStages`
      appends the semantic stage; tests.
- [x] C#: composite `SceneTreeProjection` shows each scene's script blocks (reusing
      `DialogueAstProjection`) and adds `RefKey` cross-links on speaker/jump nodes; tests.
- [x] C#: `NodeDescription`/`DisplayNode` `TypeName` for clean scene-tree legend labels,
      and `RefKey` for speaker/jump cross-links; `GraphWalk` copies both; tests.
- [x] TS: `Stage.tables`/`refKey` types; `createEntityHighlighter`; `createSemanticView`
      (graph + resizable, collapsible stacked tables); `tree-view` emits `data-ref-key`;
      `addStageTab` routing; unit + e2e tests.
- [x] Styling for the analytics layout, the table panels, and the resizable column divider;
      help text for the Semantic tab.
- [x] Rebuild the committed `dist/report.html`; `CHANGELOG` + README touch-ups.
- [ ] **Explicit UI-correctness approval** (live preview) before merge.

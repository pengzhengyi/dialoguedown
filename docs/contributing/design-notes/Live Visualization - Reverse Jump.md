# Live Visualization — Reverse Jump

> [!IMPORTANT]
> Status: **in progress** — building the source→node reverse navigation described
> here.

## Goal and scope

Give a writer a way to go **from a place in the Source editor to the matching node
in a later compiler stage** — the reverse of the existing **Jump to source**
(node→source). Right-clicking a selection (or caret) in the Source editor offers a
**Jump to ▸ \<stage\>** submenu; choosing a stage switches to that tab and reveals
the node whose span most tightly encloses the selection, centered in the viewport.

This closes the bidirectional navigation loop. The valuable direction for writers
is source→graph and source→semantic — *"where does this choice, jump, or scene
land in the flow, and does it resolve?"* — and it is precisely the direction that
is **tedious to do by hand**: finding one small node in a large graph is hard,
while a node already shows its source span. Automating the hard direction is where
the value concentrates.

**In scope:** the reverse-navigation action, a nested context-menu affordance in
the Source editor, closest-enclosing-span matching, and reveal-and-center in the
target tab. **Out of scope (planned seam):** a dedicated global keyboard *chord*
(e.g. a leader key then a per-stage digit). A keybinding that merely *opens* the
Jump-to menu at the caret is in scope.

## Functionality checklist

- [ ] Right-clicking the Source editor shows a **Jump to** item with a **submenu**
      of the available compiler-stage tabs.
- [ ] Choosing a stage switches to that tab (save-safe) and selects the node whose
      span most tightly encloses the current selection, **centered** in view.
- [ ] Matching uses the current selection `[from, to)`; with just a caret it uses
      the caret offset. Falls back to the node enclosing `from` when no node
      encloses the whole selection.
- [ ] Stages with no node carrying a span (an unavailable or empty stage) do not
      appear as targets.
- [ ] The context menu supports one level of nested submenu with mouse and
      keyboard navigation (open with `ArrowRight`/`Enter`, leave with
      `ArrowLeft`/`Escape`).
- [ ] A keybinding opens the **Jump to** submenu at the caret (the entry point of
      the "shortcut series").
- [ ] Works in a read-only (View) editor as well as Edit — the source is always
      selectable.

## Interfaces and abstractions

| Type / function | Responsibility | Collaborators |
| --- | --- | --- |
| `findEnclosingNode(nodes, from, to)` (`enclosing-node.ts`) | Pure lookup: the tightest span-bearing node enclosing `[from, to)`, else the tightest enclosing `from`, else `null`. | `DisplayNode`, `Span` |
| `ContextMenuItem` (extended, `context-menu.ts`) | Union of an **action** item (`run`) and a **submenu** item (`submenu: ContextMenuItem[]`). | `openContextMenu` |
| `SourceJumpTarget` (`source-view.ts`) | One reverse-jump destination: a stable stage `title`/`icon` plus `run(from, to)` that resolves against the *live* stage and reveals its node. | `SourceViewOptions.jumpTargets` |
| `jumpToStage(index, from, to)` (`app.ts`) | Find the enclosing node in stage `index`, then `beginNavigation → activate(tab) → view.selectById(id, { center })`. | `findEnclosingNode`, `TreeView`, `activate` |

The Source editor stays decoupled from app internals: it renders the injected
`jumpTargets` titles and calls `run(from, to)` with the current selection. All
stage knowledge and revealing live in `app.ts`.

## Key design decisions

- **Reuse the existing reveal seam.** `TreeView.selectById(id, { center: true })`
  already selects and centers a node by id (the inspector uses it to re-select
  after a rebuild). Reverse jump is therefore *node lookup + the existing
  cross-tab handoff* (`beginNavigation` → `activate`), mirroring `jumpToSource`
  rather than inventing new machinery.
- **Label "Jump to".** The app already says **Jump to source** for cross-stage
  navigation; using **Jump to ▸ \<stage\>** keeps one verb for the whole
  bidirectional pair. (Alternative "View in" was considered; "Jump to" wins on
  vocabulary consistency.)
- **Tightest enclosing span.** Every offset sits inside at least the document-root
  node, so a target almost always exists; the *tightest* enclosing node is the
  most specific and useful. A range selection prefers the tightest node containing
  the whole `[from, to)`; if none does (the selection straddles siblings), it
  falls back to the node containing `from`. Zero-width (synthetic-caret) spans do
  not *enclose*, so they never win.
- **Save-safe navigation.** Leaving the Source tab routes through
  `source.beginNavigation`, exactly like a normal tab switch, so an Auto-save
  flushes or a Manual prompt resolves before the jump.
- **Nested submenu over a flat list.** A `Jump to ▸` flyout groups the stage
  targets under one entry (the user's requested shape and the natural anchor for a
  keyboard series), rather than scattering `Jump to X` items across the top menu.

## Error and boundary cases

- **No enclosing node in a stage** → that stage is omitted from the submenu.
- **Empty / unavailable stage** (no span-bearing nodes) → omitted.
- **Caret in inter-node whitespace** → resolves to the nearest enclosing ancestor
  (often a scene or the document root); acceptable and still informative.
- **Selection spanning two siblings** → falls back to the node enclosing `from`.
- **Read-only editor** → still selectable; jump works.
- **Stage node set changed by a save** → `run(from, to)` reads the *current* stage
  at call time, never a stale snapshot.

## Integration

```mermaid
flowchart LR
    sel["Source selection [from,to)"] -->|right-click / keybinding| menu["Jump to ▸ stage"]
    menu -->|choose stage| run["target.run(from,to)"]
    run --> find["findEnclosingNode(stage.nodes, from, to)"]
    find -->|node.id| nav["beginNavigation → activate(tab)"]
    nav --> reveal["TreeView.selectById(id, { center:true })"]
```

`app.ts` builds one `SourceJumpTarget` per available stage and passes the list to
`createSourceView`. The Source editor's `contextmenu` handler (already home to the
surround menu) adds the `Jump to ▸` entry; a new editor keybinding opens the same
submenu at the caret.

## Testability

- **`enclosing-node.ts`** — pure and heavily unit-tested: nested spans, ties,
  range vs caret, straddling selections, zero-width spans, empty input.
- **`context-menu.ts`** — unit tests for submenu open/close, keyboard nav, and
  that a leaf `run` still fires.
- **`app` / `source-view`** — a unit test drives a synthetic selection and asserts
  the target `run` resolves the right node id and calls `selectById`.
- **e2e** — right-click the Source editor, open **Jump to**, pick a stage, and
  assert the corresponding node is selected and centered in that tab.

## Open questions

- **Keyboard series depth.** This iteration ships a keybinding that *opens* the
  Jump-to submenu at the caret; a full blind chord (leader then per-stage digit)
  is deferred. Is the opener enough, or is the blind chord wanted next?
- **Target ordering.** Submenu lists stages in pipeline order. A "most specific
  match first" ordering was considered but rejected as less predictable.

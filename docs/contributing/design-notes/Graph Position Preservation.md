# Graph Position Preservation

> [!NOTE]
> Status: **implemented** (an enhancement to
> [Compilation Visualization](./Compilation%20Visualization.md)). Each stage's
> graph remembers where the reader left it. A graph the reader has adjusted keeps
> its own zoom, pan, and collapsed branches; a graph they have not yet positioned
> **inherits the current view**, so switching tabs stays at roughly the same place.
> Graphs open on a **root-centered default framing**, and the zoom toolbar takes a
> **typed percentage** or a one-click **revert**. The memory lives entirely in the
> browser; nothing new is serialized or sent to the server.
>
> Like the rest of the visualization tooling, this surface is "vibe-coded" (see
> the visualization note's maturity caveat); the core engine stays the reviewed
> surface.

## Table of contents

- [Goal](#goal)
- [Ubiquitous language](#ubiquitous-language)
- [Design](#design)
- [Key design decisions](#key-design-decisions)
- [Testability](#testability)
- [Implementation checklist](#implementation-checklist)

## Goal

The report shows one interactive graph per compiler stage, each in its own tab.
A reader zooms into a node, pans to a branch, and collapses noise to focus. Two
routine actions throw that work away today:

- **Switching tabs.** Activating a tab re-fits its graph to the container every
  time, so returning to a stage you had zoomed into resets it to the default view.
- **Hot-reloading.** A disk change (or a Live Edit save) rebuilds the stage graphs
  from scratch, so the reader's zoom, pan, and fold state vanish on every edit —
  exactly when staying put matters most.

Make each stage's graph **spatially stable**, with a hybrid memory that favors
continuity by default. A graph the reader has adjusted keeps its own camera and
fold across tab switches and hot-reloads. A graph they have not positioned inherits
the **current** camera — wherever they are now — so moving between tabs does not
jump the view around. Graphs open on a readable, root-centered default rather than
a whole-graph shrink-to-fit, and the reader can dial in an exact zoom or revert a
graph to its default.

In scope:

- A **hybrid camera**: a per-graph override once a graph is adjusted, otherwise the
  shared **current** camera an untouched graph inherits.
- Per-graph **fold state** (which nodes are collapsed), independent of other graphs.
- Re-applying both across tab switches and hot-reloads, instead of re-framing.
- A **default framing** that anchors the root near the left, vertically centered, at
  a readable 100%.
- A zoom toolbar that accepts a **typed percentage** and a **Revert** control that
  restores the default and drops the graph's remembered position.

Out of scope:

- Persisting position across a full browser reload (F5). The memory is per page
  load, matching the report's other in-browser state (selection, theme aside).
- Remembering the selected node or the open detail panel across rebuilds
  (selection is still cleared on tab change, as today).
- Any change to the layout algorithm, the graphs' content, or the server payload.

## Ubiquitous language

| Term | Meaning |
| --- | --- |
| **Camera** | A graph's zoom scale and pan translation — the D3 zoom transform `{ k, x, y }` applied to the viewport. |
| **Override** | A camera a graph pins for itself the moment the reader adjusts it; it survives tab switches and hot-reloads regardless of the shared camera. |
| **Current camera** | The shared camera reflecting wherever the reader is now. An untouched graph inherits it, so switching tabs stays at roughly the same view. |
| **Fold state** | The set of node ids currently collapsed (their children hidden) in a graph's tree; always per-graph. |
| **Default framing** | The camera an untouched graph falls back to when there is no override or current camera: a readable 100% with the root anchored near the left and vertically centered. |
| **Camera store** | The in-browser memory holding each graph's override and fold, plus the one shared current camera. |
| **Stage title** | (Existing.) The stable tab label (`Markdown AST`, `Dialogue AST`, …) used as the store's per-graph key. |

## Design

The tree view owns its camera (a D3 zoom behavior) and its fold state (per-node
`_children`). This enhancement lets it apply a given camera/fold and report the
reader's own adjustments, and adds a small hybrid memory beside the tabs.

```text
   reader gesture (pin)        reveal a tab / rebuild
        │ onCameraChange(t, true)     │ cameraFor(title) = override ?? current ?? default
        ▼                             ▼
   ┌───────────────┐   adjustCamera / setFold   ┌──────────────────────────┐
   │   TreeView    │ ─────────────────────────▶ │       CameraStore        │
   │ (zoom + fold) │ ◀───────────────────────── │ overrides · folds ·      │
   └───────────────┘   applyView(camera, fold)  │ one shared current       │
                                                └──────────────────────────┘
```

### Tree view: apply a view, report adjustments

`createTreeView` takes an initial camera/fold and a set of hooks, and exposes one
method:

- `applyView(camera, fold)` — set the fold (best-effort by id), re-lay-out, and
  apply the camera; a `null` camera uses the default framing.
- `onCameraChange(transform, byUser)` — fired on every zoom; `byUser` is true for
  reader gestures (wheel, drag, the zoom controls) and false for programmatic
  applies (a reveal, the default framing).
- `onFoldChange(collapsed)` — fired when the reader collapses or expands a node.
- `onRevert()` — fired when the reader clicks Revert.

Reader gestures are distinguished from programmatic applies with the D3 event's
`sourceEvent` plus a flag the zoom controls set, so the app can pin an override
only for real adjustments.

### Camera store: a hybrid memory

`GraphCameraStore` holds a per-graph **override** map, a per-graph **fold** map, and
one shared **current** camera. `cameraFor(title)` returns the graph's override, else
the current camera, else `null` (use the default framing). `adjustCamera` pins an
override and moves the current camera; `noteCamera` moves only the current camera
(for programmatic applies); `reset` drops a graph's override and fold. It is pure,
with no DOM or D3 dependency, so it is unit-tested directly.

### App wiring: record live, apply on reveal

`runApp` wires each graph's callbacks to the store and applies the store on reveal:

- **Reader adjusts a graph** → `onCameraChange(byUser)` pins an override
  (`adjustCamera`) or, for programmatic applies, only moves the current camera
  (`noteCamera`); `onFoldChange` records the fold; `onRevert` resets the graph.
- **Reveal / hot-reload** → `applyView(cameraFor(title), foldFor(title))` shows the
  graph's own override, the inherited current camera, or the default framing.

Because every adjustment is recorded live, there is no snapshot-before-teardown
step: a rebuilt graph simply reads its remembered state from the store. The Source
tab has no graph and no camera, so it is skipped throughout.

### Default framing and the zoom toolbar

The default framing anchors the root near the left edge and vertically centered at
a readable 100%, so the reader starts at the root with its subtree filling the
viewport rightward — rather than a whole-graph fit that shrinks large trees. It is
applied once the tab's container has a real size (a just-shown tab reads zero until
it lays out), retried per frame and capped, and conditional by a generation token so a
stale retry cannot clobber a camera a later reveal has applied.

The zoom toolbar's ratio becomes a number input the reader types a percentage into
(clamped to the zoom extent), alongside the `−`/`+` steppers and a **Revert** button
that restores the default framing and clears the graph's remembered position.

## Key design decisions

- **D1 — Hybrid: inherit by default, pin on adjust.** An untouched graph inherits
  the shared current camera so switching tabs stays at roughly the same view;
  adjusting a graph pins its own override so it keeps its place. This favors
  continuity while still letting each graph diverge, which is the balance the reader
  asked for.
- **D2 — Fold stays per-graph.** Collapsed node ids only make sense within one
  graph (nodes differ between stages), so fold is never shared — only the camera is.
- **D3 — Key overrides and fold by stage title.** Titles are stable across rebuilds
  and unique per report, so they map a rebuilt stage back to its remembered state
  without threading ids through the payload.
- **D4 — In-browser, per page load.** The store is plain in-memory state. Position
  survives tab switches and hot-reloads but resets on a full page reload, like the
  report's other view state. No `localStorage`, no payload change — the offline
  single-file report stays self-contained.
- **D5 — Record adjustments live, not on teardown.** The tree view reports camera
  and fold changes through callbacks as they happen, so the store is always current
  and a rebuild simply reads it — no snapshot-before-teardown step, and reader
  gestures are told apart from programmatic applies so only real adjustments pin.
- **D6 — Root-centered default over shrink-to-fit.** A whole-graph fit makes large
  trees tiny; anchoring the root at a readable 100% starts the reader where the
  graph begins. The tradeoff — deep nodes start off screen — is acceptable because
  the reader pans and zooms to explore, and a typed percentage plus Revert make
  moving around and resetting easy.
- **D7 — A pure store module for testability.** The hybrid memory is a
  dependency-free module covered by fast unit tests; the DOM/D3 glue that applies a
  view and reports adjustments stays in the Playwright-tested tree view and app,
  matching how the rest of the browser-integration code is verified.

## Testability

- **Unit (Vitest).** `GraphCameraStore` is covered directly: an untouched graph
  falls back to the default, an override pins and is shared as the current camera,
  an adjusted graph keeps its own while others inherit the latest, `noteCamera`
  moves the current without pinning, fold is per-graph, and `reset` drops the
  override and fold. The zoom controls are covered too: rendering the input and
  Revert, committing a typed percentage on change and Enter, ignoring an invalid
  value, and reflecting the scale unless the input is focused.
- **End-to-end (Playwright).** The apply-and-report glue lives in `tree-view.ts`
  and `app.ts`, exercised in a real browser:
  - The default view frames the root in the left portion, near the vertical center.
  - The zoom input reflects, sets, and reverts the zoom.
  - A stage keeps its zoom when you leave the tab and come back.
  - A graph keeps its zoom across a hot reload.
  - An untouched graph inherits the current zoom while an adjusted one keeps its own.

## Implementation checklist

- [x] `GraphCameraStore` (pure hybrid module): `cameraFor` / `foldFor` /
      `adjustCamera` / `noteCamera` / `setFold` / `reset`, plus `CameraTransform`.
- [x] `TreeView.applyView`, the `onCameraChange` / `onFoldChange` / `onRevert`
      hooks and initial camera/fold on `createTreeView`, with reader-gesture
      detection.
- [x] Root-centered default framing (real-size retry, generation-token conditional).
- [x] Zoom toolbar: editable percentage input and a Revert button.
- [x] `runApp` records adjustments live and applies the store on reveal / rebuild.
- [x] Vitest coverage for the store and the zoom controls.
- [x] Playwright e2e for default framing, the zoom input + revert, tab-switch and
      hot-reload persistence, and the inherit-vs-pinned hybrid.
- [x] Rebuild the committed report bundle; confirm it is deterministic.

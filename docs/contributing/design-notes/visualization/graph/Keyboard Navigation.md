# Keyboard Navigation

> [!NOTE]
> Status: **implemented**. The graph tabs answer the keyboard along the graph's
> own edges: → the first way out, ← back the way the reader came, and the digits
> the numbered ways out and in. A jump is reachable without a mouse.
>
> Like the rest of the visualization tooling, this surface is "vibe-coded" (see
> the visualization note's maturity caveat); the core stays the reviewed surface.

## Table of contents

- [Goal](#goal)
- [The key map](#the-key-map)
- [Key design decisions](#key-design-decisions)
- [Known limits and tradeoffs](#known-limits-and-tradeoffs)
- [Testability](#testability)

## Goal

Before this, the arrow keys moved through the drawing's layout tree — → the first
drawn child, ← the layout parent, ↑/↓ same-depth siblings. That tree is the
spanning tree the renderer chose, not the dialogue's structure, so a jump, drawn
as a `Reference` edge rather than a child, could not be reached at all.

The keys follow the stage's edges now, so every node with a route in or out is
reachable without a mouse, and a key's way out is the inspector's. A node held
only by a placement link has no route in or out, so every key is a no-op on it.

## The key map

| Key | Action |
| --- | --- |
| → | The first way out. |
| ← | Back along the route the keyboard took, or, with no trail, the first way in. |
| ↑ / ↓ | The previous or next sibling in the drawing, wrapping at the ends. |
| 1–9 | The nth way out, once a node is chosen — on every stage, since a tree's ways out are its children. |
| Shift+1–9 | The nth way in. **Dialogue Graph only.** |
| Enter / Space | Fold or open: the scene under the pointer or chosen on the Dialogue Graph, the chosen node on a tree. |
| An arrow, nothing chosen | Select the root. |
| A click | Start a new origin: forget the trail. |

## Key design decisions

**The edges lead, not the layout tree.** The old map read the drawing's hierarchy,
whose sibling order is an accident of which route reached a node first — and a
jump was not in it at all. The keys and the inspector now name the same lists.

**← is Back, not "the layout parent".** A graph is a DAG, so a node may be led to
from several places and the layout's parent is an artifact of the spanning tree.
→ and either digit direction leave the node they left on the trail, so ← retraces
them; with no trail — a click, a restore, a table selection — Back falls back to
the first way in.

**↑/↓ step between drawn siblings.** The flow, the drawing's space, and one named
edge each get their own keys. Siblings are read after the region tiers are placed
(see [Region-Aware Graph Layout](Region-Aware%20Graph%20Layout.md)), so ↑/↓ step
where the drawing shows them, and a folded scene's box takes part like any node.
The move is spatial, not along the flow, so it forgets the trail.

**`DisplayGraph.Nests` decides whether Shift is claimed.** The stage declares
whether "the nth way in" is a question: `Nests` is false when a `Child` edge is
the spanning tree the flow is *drawn* with rather than what contains what. The
Dialogue Graph is that stage, so Shift+digit is claimed only there — not by the
tab title, a host's choice, nor by edge colors a tree's edges can also wear.

**Digits address the inspector's numbered tables.** Both neighbor tables carry a
`#` column numbered from 1 in the stage's edge order, so on the Dialogue Graph the
row a reader can see is the row a digit takes. Tree stages pass no neighbor lists,
so their digits work without a table to read them from. The number comes from
`event.code`; Shift turns `2` into `@`, and the reader pressed a key position.

**Each tab's help describes its own map.** A tree stage's help names the children
its digits take and omits ways in; the Dialogue Graph's adds them, in the
inspector's words. No reader meets a key that does nothing in front of them.

## Known limits and tradeoffs

- **A tenth way out is unreachable by keyboard.** The digits stop at nine, and a
  prefix key — press a leader, then any number — is the follow-up.
- **Numpad digits are not bound.** `digitOf` matches the number row's
  `Digit1`–`Digit9` codes, so a numeric keypad stays unclaimed.
- **A sibling move forgets the trail**, as does a click or a table selection.
  Back is a flow key; after a spatial step ← starts at the first way in.
- **Announcing a traversal is deliberately deferred.** No live region says "took
  the first way out", so a screen-reader user hears the selection, not the route.

## Testability

| Level | Covers |
| --- | --- |
| Vitest — `app.test.ts` | → follows the stage's first edge, not the layout's first child; ↑/↓ wrap and no-op without siblings; the trail walks back, including after a digit jump; the first way in is the fallback; a click forgets the trail; digits are read from the key's code; Shift+digit is ignored on a tree and honored on the Dialogue Graph; numpad, Ctrl, and Cmd stay with the browser; a digit with no node or beyond the list does nothing; folding stays with Enter/Space; each stage gets its help; other tabs and text fields keep their keys. |
| Vitest — `tree-view.test.ts` | A digit takes the nth child on a tree; a tree keeps Shift even when its edges wear route colors; a flow stage honors Shift even when no edge names its route; a folded scene's box is reachable through its edge and counts among its siblings; a leaf with nothing leading in or out stays put. |
| Playwright — `e2e/graph-keys.spec.ts` | The Dialogue Graph takes the nth way in with Shift and the nth way out with a digit; a tree takes the nth child and leaves Shift alone; the `#` header fits its gutter; each stage's help names its own map. |
| Playwright — `e2e/playbook.spec.ts` | Enter or Space pressed on a focused way-out control reveals the node it names, so the activation keys behave the same beside the graph as on it. |
| Playwright — `e2e/narrow-layout.spec.ts` | The `#app` zero-basis fix that shipped beside the keymap: in a short window with the tallest inspector, the status line keeps its height and stays inside the footer. |

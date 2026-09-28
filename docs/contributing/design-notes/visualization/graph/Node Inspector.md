# Node Inspector

> [!NOTE]
> Status: **implemented**. Clicking a graph node shows the source it came from and a rendered
> preview in a **read-only** details panel, with a **Jump to source** action that opens the Source
> tab with the node's span selected. Editing happens only in the Source tab.

## Table of contents

- [Goal and scope](#goal-and-scope)
- [Ubiquitous language](#ubiquitous-language)
- [Functionality checklist](#functionality-checklist)
- [Interfaces and abstractions](#interfaces-and-abstractions)
- [Key design decisions](#key-design-decisions)
  - [D1 — One editing surface: the Source tab](#d1--one-editing-surface-the-source-tab)
  - [D2 — Both panels render through one function](#d2--both-panels-render-through-one-function)
  - [D3 — Jump by offset span, never by text search](#d3--jump-by-offset-span-never-by-text-search)
  - [D4 — A synthetic node has a position but no text](#d4--a-synthetic-node-has-a-position-but-no-text)
- [Error and boundary cases](#error-and-boundary-cases)
- [Integration](#integration)
- [Testability](#testability)

## Goal and scope

A reader studying a compiler stage asks two questions of a node: *what text did this come
from?* and *where is that text?* The inspector answers the first inline and the second with
one click.

In scope:

- A **read-only** details panel per selected node: its category dot and label, its
  attributes, the **source** slice it was produced from, and a rendered **preview**.
- A **Jump to source** action beside the node's title that activates the Source tab and
  selects the node's span, routed through the session's save-safe navigation guard.
- One rendering for both node-details panels — the AST graph tabs' inspector and the
  Semantic Model tab's — so a node reads the same wherever it is selected.

Out of scope:

- **Editing in the panel** — see [D1](#d1--one-editing-surface-the-source-tab).
- **Inserting at a synthetic node.** A synthetic node's zero-width span makes an insert
  expressible, but the UX for what may legitimately be inserted is unsettled.

## Ubiquitous language

| Term | Meaning |
| --- | --- |
| **Node inspector** | The details panel for the selected graph node. Read-only. |
| **Span** | A node's half-open `[start, end)` character range into the document. A zero-width span is a caret position, not a selection. |
| **Slice** | The document text at a node's span — what the inspector shows as *Source*. |
| **Synthetic node** | A node a stage inserted (a filled default speaker). It has a position but no text of its own. |
| **Jump to source** | Activating the Source tab with a node's span selected (or its caret placed). |

## Functionality checklist

- [x] Selecting a node shows its category, attributes, source slice, and rendered preview.
- [x] The panel is **read-only** on every tab, in both View and Edit.
- [x] **Jump to source** selects the node's span in the Source editor and focuses it.
- [x] A **synthetic** node shows an inserted-by-the-compiler note instead of an empty
      Source block, and its jump **places the caret** where it belongs.
- [x] The jump is offered whenever there is a Source tab to land in — including a static
      export, whose read-only editor is still selectable — and hidden for a node with no
      position at all.
- [x] The AST inspector and the Semantic tab's panel render identically.
- [x] A recompile keeps the selection on the same node id rather than closing the panel.

## Interfaces and abstractions

| Type | Responsibility | Collaborators |
| --- | --- | --- |
| `nodeDetailTitle` / `nodeDetailBody` | Render a node's title and body HTML. The single rendering both panels share. | `DisplayNode`, `renderNodePreview` |
| `createDetailPanel` | Host the shared rendering in the app shell's `#detail` panel (AST graph tabs). | `nodeDetailBody`, `createJumpButton` |
| `createNodeDetailPanel` | Host the same rendering in the Semantic tab's collapsible table column. | `nodeDetailBody`, `createJumpButton` |
| `createJumpButton` | The icon button beside a node's title; hidden for a node with no span. | `Span`, `DisplayNode` |
| `Span` (`model.ts`) | A node's `[start, end)` range, beside `DisplayNode.span` it describes. | `DisplayNode` |

## Key design decisions

### D1 — One editing surface: the Source tab

The panel is read-only on every tab, and **Jump to source** lands on the exact span in one
click, so reaching a node's text never needs a second editor. Three things follow:

- **The top of the tree stays readable.** The root Document node's span is the whole script; in
  the panel it is a preview, not a cramped copy of the Source editor.
- **Every panel behaves alike.** Clicking a node never lets a reader type on one tab and not
  another.
- **Selection needs no navigation guard.** A tab switch resolves unsaved edits before it
  completes, so a graph tab is never reached with a dirty buffer, and selecting a node cannot
  leave unsaved work behind.

### D2 — Both panels render through one function

The two panels differ only in where they mount — the app shell's `#detail` aside, versus a
collapsible panel pinned above the Semantic tab's tables. Both call `nodeDetailTitle` and
`nodeDetailBody`, so a change to how a node reads lands in both by construction rather than by
remembering to.

### D3 — Jump by offset span, never by text search

The jump selects `[start, end)` directly. A text search for the node's slice would be
ambiguous the moment a document repeats a line (`Guide: Hi.` twice), and the span is already
computed server-side and carried on `DisplayNode`.

### D4 — A synthetic node has a position but no text

A node a stage inserted — the default speaker filled in for a speaker-less line — maps to no
source text, so the inspector shows a note saying so rather than an empty Source block. It
still carries a **zero-width** span, so its jump places the caret where it belongs, taking the
reader to the line they would actually edit.

## Error and boundary cases

| Case | Intended behavior |
| --- | --- |
| Node with no span (rare) | The jump button is hidden; the panel still shows attributes and source. |
| Synthetic node | Inserted-by-the-compiler note; the jump places a caret. |
| Root Document node | Shows the whole document as its slice — read-only, so it is a preview, not a second editor. |
| Static export | The panel and jump both work; the Source editor it lands in is read-only. |
| Single-graph render (no Source tab) | No jump affordance is offered. |
| Recompile while a node is selected | The selection is restored by node id; a node that no longer exists clears the panel safely. |
| Stale span after an edit | Navigation resolves the document before a tab switch, so a jump never uses spans from an unsaved buffer. |

## Integration

- **Client:** `detail-panel.ts` renders and hosts the AST inspector; `semantic-detail.ts`
  hosts the same rendering on the Semantic tab; `app.ts` wires `jumpToSource` whenever the
  report carries a source.
- **Core:** unchanged by this component. The spans it relies on are the ones the projections
  already emit, including a synthetic node's zero-width span and a scene's heading span.
- **Live session:** unchanged. There is one document, one dirty state, and one Save — all
  owned by the Source tab.

## Testability

- **Unit** (`detail-panel.test.ts`, `semantic-detail.test.ts`): a node's title, attributes,
  source, and preview render; source is escaped; a synthetic node shows the note and no source
  block; jump fires with the node's span and hides when there is none; no editor is ever
  mounted.
- **Unit** (`app.test.ts`): a recompile keeps the selection on the same node id.
- **End-to-end** (`e2e-live/live-edit.spec.ts`): jumping from an AST node, a synthetic node, and a
  Semantic-tab node lands on the right selection or caret; a graph tab's text field does not
  steal the tree view's navigation keys.

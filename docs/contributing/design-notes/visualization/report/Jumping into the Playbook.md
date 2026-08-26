# Jumping into the Playbook

> [!IMPORTANT]
> Status: **implemented**. In the Playbook tab, a node number, a speaker's name, and the entry
> node take the reader to that place in the JSON beside them.

## Table of contents

- [Goal and scope](#goal-and-scope)
- [What a table cell stands for](#what-a-table-cell-stands-for)
- [Finding the place](#finding-the-place)
- [A node's id is not its position](#a-nodes-id-is-not-its-position)
- [Key design decisions](#key-design-decisions)
- [Boundary cases](#boundary-cases)
- [Testability](#testability)
- [Open questions](#open-questions)

## Goal and scope

The Playbook tab shows the compiled JSON on the left and tables summarizing it on the right. The
tables answer *what is in here*; the JSON answers *what does it actually say*. Until now a reader
who saw `#the-market → 33` in the Anchors table had to scroll the JSON hunting for node 33.

This note covers **going from a summary to the place it summarizes**, within the Playbook tab. It
does not add navigation between tabs — the Source tab's
[Reverse Jump](../graph/Live%20Visualization%20-%20Reverse%20Jump.md) already owns that.

## What a table cell stands for

Most cells are facts. A few are *references*, and only those become destinations:

| Table | Cell | Goes to |
| --- | --- | --- |
| Playbook | **Entry node** | the node a playthrough starts on |
| Speakers | **Name** | that speaker's object |
| Anchors | **Node** | the node the anchor lands on |

The anchor's own `#the-market` cell stays a
[copyable identifier](./Copyable%20Identifiers.md) rather than a jump: it is the thing a writer
pastes into a script, not a place in this document. One cell, one gesture.

## Finding the place

The playbook is `JsonSerializer` output with `WriteIndented`, so its shape is **exactly regular**:
two spaces per level, one property or bracket per line. That regularity is already what lets the
schema hover read a line's path off the text, and it is what lets a jump find an element without
parsing the document (see [`playbook-json`](https://github.com/pengzhengyi/dialoguedown/blob/main/src/DialogueDown.Visualization/web/src/playbook-json.ts)).

Finding an element is therefore a scan: locate the named array, then walk the lines that open a
block one level inside it. Because the walk counts *depth*, an element's own nested objects are
stepped over rather than miscounted.

## A node's id is not its position

The obvious implementation — take node 33 to be `nodes[33]` — is wrong, and the corpus says so.
`conformance/readable/node-out-of-position` carries a playbook whose node ids run `0, 5`, written
precisely so a runtime cannot get away with indexing.

So the search reads each element's own `"id"` and matches on that. A speaker, which has no id of
its own, is bound **by index at the moment the table is built** — not read off the row, because
the panels sort and filter, and a sorted table no longer has the speaker where the array put it.

That difference is the whole subtlety of this feature: *a node is found by what it says it is, a
speaker by where it was.*

## Key design decisions

| Decision | Why |
| --- | --- |
| **The cell carries the target; the surface performs the jump** | The shared table renders cells for tabs that have no document to jump into. It marks the cell and lets the Playbook tab, which owns the editor, listen. |
| **Land on the opening brace** | The `{` is where the element begins, so the reader sees the whole object rather than one property of it. |
| **Center the revealed line** | A line scrolled to the very bottom is technically visible and practically useless — the object it opens runs off the screen below it. |
| **An unresolvable target does nothing** | Better to leave the reader where they are than to send them somewhere plausible and wrong. |
| **A dotted underline, not the copy hover** | Copying and going somewhere are different promises, so they read differently. |

## Boundary cases

- **A playbook that did not compile** has no editor, so no cell is a destination.
- **A node the document does not hold** — a stale table against a re-rendered document — resolves
  to nothing and the reader stays put.
- **A sorted or filtered table** still jumps correctly, because the target was bound when the row
  was built.
- **An empty cell** is never a destination, the same rule
  [Saying Nothing](./Saying%20Nothing%20Across%20the%20Report.md) applies elsewhere.

## Testability

The finding is a pure function over an `EditorState`, so it is unit-tested against a document
whose ids are deliberately sparse: `playbook-jump.test.ts` pins that node `5` is found at the
*second* element, that id `1` resolves to nothing even though a second element exists, and that a
speaker is found by index.

The wiring is pinned separately — `playbook-view.test.ts` asserts which cells carry which target —
and the actual scroll is left to Playwright, where a browser can measure: `playbook.spec.ts`
clicks the anchor's node and asserts both the line landed on **and that it is on screen**.

## Open questions

- **Jumping the other way.** Clicking a node in the JSON could highlight its row in the tables.
  The Semantic Model already cross-links on hover, so the seam exists, but the reverse direction
  has not been asked for.
- **Keyboard reach.** A jump is a click, and a table cell is not focusable — the same gap
  [Copyable Identifiers](./Copyable%20Identifiers.md) records, and worth one pass covering both.

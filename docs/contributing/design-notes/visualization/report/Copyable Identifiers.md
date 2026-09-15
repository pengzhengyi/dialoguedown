# Copyable Identifiers

> [!IMPORTANT]
> Status: **implemented**. An `@id`, an anchor, and a jump target copy on click in every table
> that shows one. Prose does not. Extends the copying the Config tab already offered to the
> Semantic Model and the Playbook, and settles the `@` sigil left open by
> [Saying Nothing Across the Report](./Saying%20Nothing%20Across%20the%20Report.md).

## Table of contents

- [Goal and scope](#goal-and-scope)
- [Why an identifier and not everything](#why-an-identifier-and-not-everything)
- [Why not a capsule](#why-not-a-capsule)
- [The rule](#the-rule)
- [Key design decisions](#key-design-decisions)
- [Boundary cases](#boundary-cases)
- [Testability](#testability)
- [Open questions](#open-questions)

## Goal and scope

A writer reading the report keeps finding things they need to type back into a script: the `@id`
that addresses a speaker, the anchor a jump lands on, the target a jump already names. The Config
tab let them click one and get it; the other two tabs made them retype it.

This note fixes **which cells offer themselves for copying**. It does not change what the report
shows, only what a click on it does.

## Why an identifier and not everything

Making every cell copyable is the obvious generalization and the wrong one. A table holds two
kinds of thing:

| Kind | Example | A click should |
| --- | --- | --- |
| **An identifier** — a token the language defines, that a writer types verbatim | `@guide`, `#the-market` | copy it |
| **Prose** — a name, a label, a title, a count | `Guide`, `Take the east road`, `1` | do nothing |

Copying prose is worse than useless. It hands back a sentence nobody asked for, and it steals the
selection from a reader who was trying to highlight part of the text. The distinction is not
cosmetic: an identifier has one correct form, and that is exactly what makes it worth a click.

## Why not a capsule

Tags are drawn as [capsules](./Tag%20Capsules.md), and an `@id` is the obvious next candidate. It
was considered and rejected.

**A capsule's job is to delimit siblings.** A tag cell holds zero to many tags, so a capsule marks
where one ends and the next begins. An `@id` cell holds **zero or one**. There is nothing to
separate, so the shape would be decoration rather than structure.

Two reasons follow from that one:

- **Color would have to mean something.** The tag capsule's fill is the palette's `tag` hue, the
  same one the graph legend shows. There is no `id` category, and inventing a hue would spend a
  palette slot on something that is not a graph concept.
- **The `@` already identifies it.** `@guide` reads as an id on its own, where a bare `wise`
  genuinely needs its `#` to read as a tag.

A density argument was also considered — that ids are too common for capsules — and **measured
false**: across the shipped examples an `@id` appears on five of nine speakers and a tag on four.
Cardinality is the real reason, and it holds whatever the density.

## The rule

> A cell that holds an identifier copies it. A cell that holds prose does not.

A copyable cell wears the same hover cue the Config tab uses, and copies through the shared
`data-copy` listener the table already carries for tag capsules. The identifiers today:

| Table | Cell |
| --- | --- |
| Speakers | `@id` |
| Anchors | the anchor |
| Jump resolutions | the target |

## Key design decisions

| Decision | Why |
| --- | --- |
| **The projection marks the cell, not the client** | Whether a cell is an identifier is a fact about the model, known where the cell is built. The client would have to guess it back from the text. |
| **A flag, not a copy string** | An identifier's copy text *is* its display text. A second string could drift from the first, and there is no case that wants them to differ. |
| **The Playbook gained its `@`** | It wrote a bare `guide` while the other tabs wrote `@guide`. Copyability decided the tie: a bare `guide` copies something no script accepts. |
| **An empty cell is not copyable** | A speaker with no `@id` has an empty cell, per [Saying Nothing](./Saying%20Nothing%20Across%20the%20Report.md). Offering to copy nothing would be a hover cue over a blank. |

## Boundary cases

- **Selecting text in a copyable cell.** A click copies, but a drag still selects; the listener
  fires on click, so a reader who drags across an anchor keeps their selection.
- **A cell that is both copyable and cross-linked.** The `@id` cell copies while its row still
  highlights the speaker on hover — the two use different events, so neither shadows the other.
- **Jump targets that are not anchors.** A file-scoped target (`chapter-02.md#meet-bob`) is still
  the text a writer types, so it copies as written.
- **The Scene column beside an anchor** holds a heading's prose, not a target, so it stays inert.

## Reaching a cell's act

A cell that copies when it is pressed is a control, and a control has to be reachable without a
mouse. A `<td>` takes no focus and answers no key, so for a while the copy — and the table jump
that works the same way — could only be performed by pointing at it.

The rule now is one line, and it covers every surface that offers an act in a cell:

> The text of an acting cell sits inside a real `<button>`.

A native button is what makes the cell reachable: it takes focus in document order, and it turns
Enter and Space into an ordinary click of its own accord. That last part is why nothing else had
to change — the delegated `data-copy` and `data-jump` listeners the tables already carry receive
the keyboard's activation as the click they were always listening for, so there is no second code
path for the keyboard to drift out of step with.

| Decision | Why |
| --- | --- |
| **The button sits *inside* the cell** | A `<td>` given `role="button"` stops being a cell, and a reader moving through the grid loses the table. Nesting keeps both the table and the control. |
| **The cell keeps `data-copy` / `data-jump`** | The whole cell stays the mouse's target, so pointing anywhere in it still works; the button's own click bubbles to the same listener. |
| **The button is named for the act** | A button's name is what is announced on arrival, so it reads `Copy @guide`, not `@guide` — the value is already in the text. |
| **`display: inline`, not `inline-block`** | The text then wraps exactly as the bare text node it replaced, and the jump cell's dotted hover underline still reaches it. |
| **Only the focus ring is kept** | Everything else the stylesheet paints on a button is stripped: the cell around it already carries the hover cue and the accent. The ring is the point of making it focusable. |

The tag capsule is the same story told shorter — it was already a `data-copy` element, so it
simply became a button rather than gaining one.

## Testability

`semantic-table.test.ts` pins both halves of the rule: a copyable cell copies exactly once, and an
ordinary cell copies nothing. `SemanticProjectionTests` pins which cells the projection marks —
and, as importantly, which it leaves alone. `playbook-view.test.ts` pins the `@` sigil and both
identifiers.

Keyboard reach is pinned in a browser rather than in jsdom, because the claim *is* the browser's
behavior: jsdom does not turn Enter into a click, so a passing jsdom test would prove nothing
about it. `keyboard-reach.spec.ts` copies an identifier with Enter and with Space, copies a tag
capsule, walks from one acting cell to the next with Tab, checks the focus ring is painted when
the keyboard did the focusing, and confirms the cell has kept its own role. The unit tests
alongside pin the structure: the button exists, it is named for the act, and a prose cell has
none.

## Open questions

- **Column order.** Resolved: both speaker tables now read *Name, @id, Tags, Default*, so a reader
  moving between the Semantic Model and the Playbook meets the columns in the same order.
- **A copy affordance a keyboard can reach.** Resolved: a cell that acts when pressed now puts its
  text inside a real `<button>` — see [Reaching a cell's act](#reaching-a-cells-act).

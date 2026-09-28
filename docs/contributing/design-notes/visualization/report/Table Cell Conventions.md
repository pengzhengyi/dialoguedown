# Table Cell Conventions

> [!NOTE]
> Status: **implemented**. Every table in the report — the Config tab, the Semantic Model, and the
> Playbook — follows one set of cell rules: an absent value is an empty cell, a tag is a capsule,
> and a cell holding an identifier copies it through a real button.

## Table of contents

- [Goal and scope](#goal-and-scope)
- [Empty cells](#empty-cells)
- [Tag capsules](#tag-capsules)
- [Copyable identifiers](#copyable-identifiers)
- [Reaching a cell's act](#reaching-a-cells-act)
- [Testability](#testability)
- [Open questions](#open-questions)

## Goal and scope

Three tabs show the same speakers, anchors, and tags. These rules make one speaker read the same
wherever it appears, and let a writer lift an identifier straight back into a script. They govern
**cells**; an empty *table* still explains itself in prose (*"This playbook has no speakers."*).

Both speaker tables that follow these rules read the same columns in the same order:

| Name | @id | Tags | Default |
| --- | --- | --- | --- |
| Guide | `@guide` | `#wise` `#role=guide` | ✓ |
| Merchant | | `#role=merchant` | |
| (anonymous) | | | |

## Empty cells

> When there is nothing to say, say nothing.

An absent value is an **empty cell** — no dash, no `N/A`, no `(none)`. The row around it is full
and the column header says what it would hold, so a glyph would only compete with the values that
are present. A tags column with two entries and twelve blanks looks like what it is.

### D1 — A boolean's false case is blank, its true case a tick

`Default` is `✓` or blank. A tick and a blank are told apart without reading; a dash would read as
*unknown* for a fact the compiler knows.

### D2 — The anonymous speaker is named

A line with no speaker prefix belongs to the **anonymous speaker**, a real participant a runtime
voices. A blank would suggest the table failed to draw a name, so the speaker tables name it
`(anonymous)`. The test is whether a reader who sees the blank would conclude something false.

| Boundary | Behavior |
| --- | --- |
| A field/value table (`Uses` with nothing) | Empty; the label beside it carries the meaning. |
| A screen reader | Announces a blank, which is the intended meaning. |
| An empty cell in a copying column | Offers no copy and no hover cue. |

## Tag capsules

A tag is drawn as one capsule wherever the report shows it: the three speaker tables and the
Source preview's construct marks (`renderTag` in `tag-chip.ts`).

| Part | Rule |
| --- | --- |
| **Shape** | Rounded, monospace, small — a token, not a sentence. A capsule never wraps; the cell wraps between capsules. |
| **Fill** | The palette's `tag` pink for a writer's tag; violet for a reserved name. |
| **Dot** | On a custom tag only. Its hue is a hash of the tag's **name** over eight identity hues, so `#wise` wears one dot in every tab and `role=guide` shares a dot with `role=merchant`. |
| **Text** | As a script writes it: `#wise`, `#role=guide`, `##default`. |
| **Copy** | The capsule is a button carrying `data-copy`, handled by the table's own listener. |

### D3 — Kind and identity take different places

The fill already means "this is a tag" — the same pink the graph legend shows — so it cannot also
say *which* tag. Identity takes the small dot, from a separate hue set so it never reads as a
category. A reserved tag needs no dot: its violet identifies it, and the set is small.

### D4 — The hue is derived, not stored

Hashing the name is stable across reloads and needs no state. Collisions are expected with eight
hues; the dot narrows the field and the text settles it, so a color-blind reader loses nothing but
the aid.

### D5 — The projections send a tag's parts

The capsule needs a tag's name, value, and kind, so every projection emits the shared
`TagView(Name, Value, Reserved)`. `Reserved` comes from `ReservedTagNames.Known`. A tag cell also
keeps its joined text, so search, sort, and export still read it.

## Copyable identifiers

> A cell that holds an identifier copies it. A cell that holds prose does not.

| Kind | Example | A click |
| --- | --- | --- |
| **Identifier** — a token the language defines, typed verbatim | `@guide`, `#the-market` | Copies it |
| **Prose** — a name, label, title, or count | `Guide`, `Take the east road`, `1` | Does nothing |

Copying prose hands back a sentence nobody asked for and steals the selection from a reader
highlighting part of it. The copying cells:

| Table | Cell |
| --- | --- |
| Speakers (Semantic Model, Playbook) | `@id` |
| Anchors (Semantic Model, Playbook) | The anchor, as `#slug` |
| Jump resolutions | The target, including a file-scoped one such as `chapter-02.md#meet-bob` |

The Config tab's speakers table copies every value, names included — see
[Open questions](#open-questions).

### D6 — The projection marks the cell

Whether a cell is an identifier is a fact about the model, so the projection sets
`SemanticCell.Copyable`; the client does not guess from the text. A flag, not a second copy string:
an identifier's copy text *is* its display text.

### D7 — An identifier is written with its sigil

The Playbook writes `@guide`, like the other tabs, because a bare `guide` copies something no
script accepts.

### D8 — An `@id` is not a capsule

A capsule separates siblings, and an `@id` cell holds at most one value. The `@` already marks it
as an id, and there is no `id` category to give it a hue.

| Boundary | Behavior |
| --- | --- |
| Dragging across a copyable cell | Selects text; the listener fires on click only. |
| A copyable cell whose row cross-links | The cell copies and the row still highlights on hover — different events. |
| The Scene column beside an anchor | Prose, so inert. |

## Reaching a cell's act

> The text of an acting cell sits inside a real `<button>`.

This covers every cell that acts when pressed: a copy, and a [jump into the
playbook](./Navigating%20the%20Playbook.md). A native button takes focus in document order and turns
Enter and Space into a click, so the delegated `data-copy` and `data-jump` listeners receive the
keyboard's activation with no second code path.

### D9 — The button sits inside the cell

A `<td role="button">` stops being a cell and a screen-reader user loses the grid. The cell keeps
`data-copy` or `data-jump`, so the whole cell stays the mouse's target.

### D10 — Named for the act, styled as text

The button's accessible name is `Copy @guide`, not `@guide`. It is `display: inline`, so it wraps
like the text it replaced, and keeps only its focus ring. A tag capsule is itself the button.

## Testability

| Level | Covers |
| --- | --- |
| Vitest — `semantic-table` | A copyable cell copies once and a prose cell copies nothing; an acting cell holds a button named for its act. |
| Vitest — `tag-chip` | A name hashes to the same hue; a custom tag has a dot and a reserved one does not; the capsule copies its exact text. |
| Vitest — `playbook-view`, `config-view` | Empty cells for an absent id and empty tags; `✓` and blank for Default; the `@` sigil; capsule classes. |
| .NET — `SemanticProjectionTests` | `(anonymous)` for the nameless speaker; which cells are marked copyable, and which are not. |
| Playwright — `keyboard-reach.spec.ts` | Enter and Space copy an identifier and a capsule; Tab walks between acting cells; the focus ring paints; the cell keeps its role. jsdom does not turn Enter into a click, so this lives in a browser. |

## Open questions

- **The Config tab copies names.** Its speakers table copies the name as well as the `@id` and
  tags, which the [identifier rule](#copyable-identifiers) says it should not. Either the rule gains
  an exception or the Config table drops name copying.
- **The Config tab heads its id column `Id`,** where the Semantic Model and the Playbook say `@id`.
- **The anonymous speaker has two spellings.** The speaker tables write `(anonymous)`; the Playbook
  Nodes table's summary writes `<anonymous>`, following that column's angle-bracket convention for
  stand-ins ([Playbook Summary Segments](./Playbook%20Summary%20Segments.md#roles)).

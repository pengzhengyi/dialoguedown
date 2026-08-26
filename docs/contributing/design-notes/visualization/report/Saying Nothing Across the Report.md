# Saying Nothing Across the Report

> [!IMPORTANT]
> Status: **implemented**. One rule now governs every empty table cell in the report: the
> Semantic Model, the Playbook, and the Config tab all say nothing when there is nothing to say,
> and name the anonymous speaker when there is.

## Table of contents

- [Goal and scope](#goal-and-scope)
- [The problem: four ways to write nothing](#the-problem-four-ways-to-write-nothing)
- [The rule](#the-rule)
- [Absence that is a fact](#absence-that-is-a-fact)
- [Key design decisions](#key-design-decisions)
- [Boundary cases](#boundary-cases)
- [Testability](#testability)
- [Open questions](#open-questions)

## Goal and scope

Three tabs show the same speaker. The Config tab lists who a project configures, the Semantic
Model lists who the compiler resolved, and the Playbook lists who a runtime will be asked to
voice. Each grew its own way of writing an absent `@id` or an empty tag list, so one speaker read
three ways depending on which tab you were standing in.

This note fixes how the report renders **an absent value in a table cell**. It does not govern
empty *tables* — a table with no rows still explains itself in prose, which is a different
problem with a different answer.

## The problem: four ways to write nothing

Before this note, one speaker with no `@id` and no tags rendered like this:

| Tab | Name absent | `@id` absent | Tags empty | Not the default |
| --- | --- | --- | --- | --- |
| Config | *n/a* | `—` dimmed italic | `—` dimmed italic | *n/a* |
| Semantic Model | `N/A` | `N/A` | *(empty)* | *(empty)* |
| Playbook | `(anonymous)` | `—` full brightness | `—` full brightness | `—`, or `yes` |

Two things are wrong here beyond the inconsistency.

**The placeholders outweighed the data.** In a four-speaker playbook where one speaker carries an
`@id` and one carries a tag, eight of twelve cells were a dash. The column that should have drawn
the eye — the one speaker tagged `wise` — competed with a wall of punctuation, and the reader had
to look past the absences to find the presences.

**A dash and a `no` are different claims.** `Default: —` reads as *unknown*, but the compiler
knows perfectly well that the speaker is not the default. The column is a boolean, and a boolean's
false case is not missing data.

## The rule

> When there is nothing to say, say nothing.

An absent value renders as an **empty cell**. No dash, no `N/A`, no `(none)`.

The reader is not left guessing, because the row around the empty cell is full: a speaker's name
is right there, and the column header says what the empty cell would have held. Emptiness in a
grid already means absence — spending a glyph to restate it only competes with the data that is
present.

This also makes the table's shape informative at a glance. A tags column with two entries and
twelve blanks *looks* like what it is: a script where two speakers are tagged.

## Absence that is a fact

One absence survives, because it is not an absence at all.

A line with no speaker prefix belongs to the **anonymous speaker** — a real participant in the
compiled script, one a runtime resolves and voices, which happens to have no name. Its
namelessness is a fact about the script rather than a gap in the table, so the report names it
`(anonymous)` in every tab.

The test is whether a reader who sees the blank would conclude something false. For a missing
`@id`, blank is exactly right: the speaker has no `@id`. For the anonymous speaker, blank would
suggest the table failed to render a name that exists.

## Key design decisions

| Decision | Why |
| --- | --- |
| **Empty, not a dimmed dash** | The Config tab's dimmed italic dash was the least noisy of the three, and still lost to blank: a muted glyph is quieter than a bright one, but no glyph is quieter still, and it needs no styling to stay quiet. |
| **`✓` for the default speaker, blank otherwise** | `yes`/`—` asked the reader to compare two tokens; a tick and a blank are told apart without reading. The Semantic Model already did this, so the Playbook adopted it rather than inventing a third form. |
| **`(anonymous)` over `N/A`** | `N/A` says the report has nothing; `(anonymous)` says the script has a nameless speaker. The Playbook was already right, so the Semantic Model adopted its wording. |
| **The rule is per-cell, not per-tab** | Each tab was already right about something and wrong about something else. Writing the rule down once means the next table gets it right without consulting three precedents. |

## Boundary cases

- **A field/value header row.** `Uses` with nothing after it is still legible, because the label
  sits in the adjacent cell and carries the meaning. The same rule applies.
- **An empty table.** Out of scope: a table with no rows keeps its explanatory prose
  (*"This playbook has no speakers."*), which answers a question a blank cell never raises.
- **A screen reader.** An empty cell is announced as blank, which is the intended meaning. A dash
  is announced as "em dash", which is not.
- **Copy to clipboard.** The Config tab's cells are click-to-copy. An empty cell has nothing to
  copy and no copy affordance, which is correct — previously a reader could copy a dash.

## Testability

Each surface owns a test that fails if it drifts back:

- `playbook-view.test.ts` pins an empty cell for an absent id and an empty tag list, `✓` for the
  default speaker and blank for the others, and an empty `Uses`.
- `config-view.test.ts` pins empty cells for a speaker with neither an id nor a tag.
- `SemanticProjectionTests` pins `(anonymous)` for the nameless speaker and an empty `@id`.

Each was written first and confirmed to fail against the previous rendering.

## Open questions

- **The `@` sigil.** The Config tab and the Semantic Model write an id as `@guide`, exactly as a
  script references it; the Playbook writes the bare `guide`, because a playbook is data a runtime
  keys on rather than syntax a writer types. Both readings are defensible, so the difference is
  left standing rather than settled by this note.
- **Column order.** The Semantic Model orders speakers *Name, @id, Tags, Default*; the Playbook
  orders them *Name, Id, Default, Tags*. Aligning them would help a reader moving between tabs,
  but it is a layout question rather than an absence one.

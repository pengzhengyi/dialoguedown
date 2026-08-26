# Tag Capsules

> [!IMPORTANT]
> Status: **implemented**. One capsule draws a tag wherever the report shows it — the Config tab,
> the Semantic Model, and the Playbook — colored by kind, with its identity in a leading dot.
> Follows [Saying Nothing Across the Report](./Saying%20Nothing%20Across%20the%20Report.md), which
> settled how the same tables render an *absent* value.

## Table of contents

- [Goal and scope](#goal-and-scope)
- [The problem: one tag, three renderings](#the-problem-one-tag-three-renderings)
- [Two things color has to say](#two-things-color-has-to-say)
- [The capsule](#the-capsule)
- [Key design decisions](#key-design-decisions)
- [What the data had to become](#what-the-data-had-to-become)
- [Boundary cases](#boundary-cases)
- [Testability](#testability)
- [Open questions](#open-questions)

## Goal and scope

A tag is how a writer hands a host something it needs — a portrait, a voice, a mood. The same tag
appears in three tabs, and this note fixes how the report **draws** one. It does not change what a
tag means, how it is parsed, or which names are reserved.

## The problem: one tag, three renderings

The Config tab already drew a capsule: rounded, monospace, colored by kind, click-to-copy. The
other two tabs did not.

| Tab | How a tag was drawn | Copyable |
| --- | --- | --- |
| Config | A capsule, pink for custom and violet for reserved | Yes |
| Semantic Model | `#wise #role=guide` joined into one string | No |
| Playbook | `role=guide` — joined, and without the `#` a script writes | No |

Two costs. A reader moving between tabs met the same tag as three different objects. And in the
two joined renderings the `=` in `role=guide` disappeared into a run of text, so a keyed tag was
hard to pick out from a bare one.

## Two things color has to say

The report already spends color on meaning. [`palette.ts`](https://github.com/pengzhengyi/dialoguedown/blob/main/src/DialogueDown.Visualization/web/src/palette.ts)
maps each cross-stage category to a hue, and **`tag` is pink** — the same pink the graph legend
shows beside "Tag". The Config capsule was already wearing it, deliberately.

So a request to color capsules *by identity* — every `#wise` the same color, different from
`#mood` — collides with a hue that already means "this is a tag". Spending the capsule's fill on
identity would leave a green tag next to a green speech node meaning nothing alike.

The resolution is that the two claims are different in kind, so they get different real estate:

| Claim | Where it lives | Why |
| --- | --- | --- |
| "This is a tag, and reserved or not" | The capsule's fill and border | It is the same claim the graph legend makes, so it keeps the palette's hue. |
| "This is *`#wise`*, the one you saw upstairs" | A small leading dot | An identity has no meaning to preserve, so it can take an arbitrary hue without spending one. |

## The capsule

- **Shape.** Rounded, monospace, small — a token, not a sentence.
- **Fill.** The palette's `tag` pink for a writer's own tag; violet for a reserved name
  DialogueDown owns, because a reserved name is one of a closed set and its kind *is* its identity.
- **Dot.** Only on a custom tag. Its hue comes from hashing the tag's **name**, so `#wise` wears
  the same dot in every table and every tab across reloads, and `role=guide` shares its dot with
  `role=merchant` — the tags a reader thinks of as related look related.
- **Text.** Written as a script writes it: `#wise`, `#role=guide`, `##default`.
- **Copy.** The whole capsule carries `data-copy`, so a click lifts the tag verbatim into a script.

## Key design decisions

| Decision | Why |
| --- | --- |
| **Hash the name, not the whole label** | `role=guide` and `role=merchant` are the same axis, so they should read as a family. Hashing the label would scatter them. |
| **A separate, small identity palette** | Reusing `CATEGORY_COLORS` would say a tag *is* a jump or a scene. The identity hues are their own set, chosen to stay apart and legible on both themes. |
| **Derive the hue, do not store it** | A stored color drifts from the tag and needs migrating. Hashing is stable across reloads and needs no state. |
| **No dot on a reserved tag** | Its violet already identifies it, and the closed set is small enough to learn. A dot would imply a variety that does not exist. |
| **Keep the cell's plain text** | A tag cell still carries its joined text, so search, sort, and export read the cell even though the drawing differs. |

## What the data had to become

The capsule needs a tag's parts, and two projections were throwing them away — one joined tags
into a display string, the other flattened each into `name=value`. Both now emit a shared
`TagView(Name, Value, Reserved)`, which also retired the Config tab's private copy of that type.

Reserved-ness is derived from `ReservedTagNames.Known`, the compiler's single source of truth,
rather than re-listed for the report.

## Boundary cases

- **A speaker with no tags** keeps an empty cell, per
  [Saying Nothing Across the Report](./Saying%20Nothing%20Across%20the%20Report.md).
- **Two names colliding on one hue** is expected and harmless: the dot narrows the field, the text
  settles it. With eight hues a collision is likely well before a script has eight tags, so the
  dot is an aid, never the identifier.
- **A very long tag** does not wrap inside its capsule; the cell wraps between capsules instead,
  so a capsule is never broken across lines.
- **Color-blind readers** lose the dot's distinction but lose nothing else: every capsule still
  spells its tag, and kind is carried by fill *and* the `#`/`##` prefix.

## Testability

`tag-chip.test.ts` pins the parts a reader depends on: a name hashes to the same hue every time,
different names differ, a custom tag gets a dot and a reserved one does not, and the capsule
carries the exact text to copy. `playbook-view.test.ts` pins that the Playbook draws capsules and
writes a tag with its `#`, and `config-view.test.ts` pins the kind classes.

## Open questions

- **Tags elsewhere.** Only speaker tables draw capsules today. Line tags in the Dialogue AST and
  the graph's node details still render as text; whether they should is a separate question about
  those surfaces, not about the capsule.
- **Filtering by tag.** A capsule is an obvious click target for "show me every speaker tagged
  `#wise`". The Speakers table already facets on Default, so the seam exists — but a tag facet is
  a table feature rather than a drawing one, and is left for when someone wants it.

# Playbook Summary Segments

> [!NOTE]
> Status: **implemented**. A playbook node's **summary** travels as labeled **segments** — each
> piece says what it is and, when it names a node, where it leads — so the
> [Nodes table](./Playbook%20Nodes%20Table.md) draws what the projection wrote and never re-parses a
> line a writer's punctuation could break.

## Table of contents

- [Goal and scope](#goal-and-scope)
- [Ubiquitous language](#ubiquitous-language)
- [Why a role is needed](#why-a-role-is-needed)
- [The summary grammar](#the-summary-grammar)
- [Roles](#roles)
- [Writing a summary as segments](#writing-a-summary-as-segments)
- [Interfaces and abstractions](#interfaces-and-abstractions)
- [Key design decisions](#key-design-decisions)
- [Error and boundary cases](#error-and-boundary-cases)
- [Testability](#testability)

## Goal and scope

This note owns the summary: its grammar, its wire shape, its roles, and how the client colors,
lists, and links them.

**In scope:** the segment list on `PlaybookNodeView`; the projection that writes it from the typed
playbook model; the client that draws each segment by role, draws a list as a list, explains a
piece on hover, and makes a piece that names a node a way to reach it.

**Out of scope:** the playbook format and `SpeechText`'s published flattening
([Speech as Plain Text](../../runtime/Speech%20as%20Plain%20Text.md)).

## Ubiquitous language

| Term | Meaning |
| --- | --- |
| **Segment** | A piece of a summary's text together with the role it plays. A summary is an ordered list of segments. |
| **Role** | What a segment is: a speaker, the writer's words, the table's grammar, a boundary, a command, a query, an absent marker, or a node the summary names. |
| **Summary** | The whole line a node reads as — the segments' text joined end to end. |
| **Grammar** | The table's own words and punctuation: keywords, separators, list boundaries, and the cap's ellipsis. |
| **Boundary** | A segment that divides a list's items or opens the list. The client draws it as the break. |
| **Introduction** | The pieces before a list's first boundary — a condition, a draw's header. They read on the cell's first line. |
| **Target** | The node a segment names, for a segment that can be followed. |
| **Absent marker** | A name in angle brackets the report writes where a value is missing: `<no speech>`, `<no label>`. |
| **Drawn segment** | The client's own `SemanticSegment`: a text and a class, where the wire segment carries a role. |

"Segment", not "run": the domain spends "run" on a playthrough. It also avoids `span` (source),
`fragment` (speech), and `token` (highlighting).

## Why a role is needed

Every delimiter a summary uses — `||`, `;`, `,`, `:`, `IF`, `{…}`, `<…>` — is a character a
writer can type. A choice whose second option is `Go left || right` cannot be told from a
three-option choice by reading the line:

```text
| # | Kind   | Summary                              | Leads to |
| 4 | choice | Take the road \|\| Go left \|\| right | 30, 31   |
```

The projection composes the summary from parts it already holds, so it labels each part instead of
leaving the client to split the line again.

## The summary grammar

The column reads as small pseudocode in four registers. **Capitals** are what the table asserts.
**Angle brackets** stand where a value is missing or cannot be resolved (`<anonymous>`,
`<unknown>`, `<no speech>`, `<no label>`). **Round brackets** are always a command —
`(fade in)` for an action the host knows, `ShowBackground(tavern, firelit)` for one the script
names. Everything else came from the script. Case keeps the table's words apart from a writer's,
so speech needs no quotation marks.

| Kind | Summary |
| --- | --- |
| `line` | `Keeper: North, past the burned mile.` |
| `control` with commands | `(fade in)`, `ShowBackground(tavern, firelit)` as a numbered list |
| `control`, a bare jump | `⇒ The Mountain Road` |
| `control`, neither | `CONTINUE` |
| `choice` | Its options as a bulleted list; a conditional option ends `IF Alice.HasMap?` |
| `random-choice` | `DRAW 1 FROM 2:` then each arm's odds, as a bulleted list |
| `branch` | `IF FoundKey? THEN 12 ELSE 14` |
| `end` | `END` |
| A node with its own condition | Led by it: `IF Hero.IsBrave? THEN Keeper: …` |

A branch pairs each condition with its node, which `Leads to` cannot express. A random choice shows
odds rather than words, because an arm carries no words: the engine picks. A summary is capped at
200 characters.

## Roles

A role is assigned where the projection knows the part's job; nothing is inferred from characters.

| Role | Assigned to | Drawn as |
| --- | --- | --- |
| `speaker` | A line's speaker name, including the `<anonymous>` and `<unknown>` stand-ins | `.dd-sum-speaker` |
| `plain` | The writer's words: speech minus its queries, an option's label | The cell's own color |
| `keyword` | `IF`, `THEN`, `ELSE`, `END`, `CONTINUE`, `DRAW 1 FROM n` | `.dd-sum-keyword` (muted) |
| `separator` | The speaker's and the odds' colon, the jump arrow `⇒`, the cap's ellipsis | `.dd-sum-separator` (muted) |
| `boundary` | The punctuation dividing a list's items, or an empty opening | The list break |
| `command` | `(fade in)`, `ShowBackground(tavern, firelit)` | `.dd-sum-command` |
| `query` | A `{Key}` for a real `QueryFragment`, and every condition key | `.dd-sum-query` |
| `absent` | `<no speech>`, `<no label>` | `.dd-sum-absent` (muted italic) |
| `target` | A node the summary names: a branch arm's number, a jump's words | A control that reveals the node |

The set is the `SummaryRole` union in `model.ts` and `SummaryRoles` in C#; every role is a key of
`SUMMARY_SEGMENT_CLASS`.

The roles stay coarse on purpose: naming an option or condition apart from the writer's words would
rebuild the graph's structure inside a payload meant to read as one line. A client that wants the
structure reads the playbook.

## Writing a summary as segments

```text
segmentsOf(node, speakers):
    line          -> speaker(nameOf(line.speaker)) separator(": ") speechSegments(line.speech)
    control       -> items(commands) if there are any
                     otherwise -> separator("⇒ ") target(jump words) | keyword("CONTINUE")
    choice        -> items(option per option)
    random-choice -> keyword("DRAW 1 FROM n") separator(": ") items(odds per arm)
    branch        -> keyword("IF") condition keyword("THEN") target(node), and so on
    end           -> keyword("END")

    # A node-level condition leads:
    keyword("IF") condition keyword("THEN") body

    # A list opens with an empty boundary, so what precedes it is its introduction;
    # a lone item writes no boundary and reads as a line.
    items(xs) -> boundary("") x1 boundary(sep) x2 …

    nameOf(i)  -> the speaker's name, or "<anonymous>" when unnamed, or "<unknown>"
                  when i is outside the list — all with the speaker role
    option     -> plain(label) | absent("<no label>"), then keyword("IF") condition when conditional;
                  every piece of the label carries the option's target
    condition  -> query(key + "?")

speechSegments(fragments):
    TextFragment       -> plain(text)
    QueryFragment      -> query(placeholder)
    StyledTextFragment -> speechSegments(children)
    LinkFragment       -> speechSegments(label)
    ImageFragment      -> speechSegments(alt)
    LineBreakFragment  -> plain(" ")
    anything else      -> nothing          # the fragments SpeechText drops
    # Adjacent plain segments with the same target merge.
```

## Interfaces and abstractions

| Type | Responsibility |
| --- | --- |
| `PlaybookSegmentView(Text, Role, Target?)` | One segment, with a factory per role (`Speaker`, `Plain`, `Boundary`, `Opening`, `LinkedTo`, …). Its own file. |
| `SummaryRoles` | The role names the wire carries. |
| `PlaybookNodeSummary.SegmentsOf(node, speakers)` | Writes a node's segments, including the speech walk and the cap. |
| `PlaybookSegmentView`, `SummaryRole`, `SemanticCell.list` (`model.ts`) | The wire shape, the role union, and a cell drawn as a list. |
| `summaryCell`, `summaryList`, `drawnSegment` (`playbook-view.ts`) | Join segments into the cell's text, map roles to classes, split at boundaries, and turn a target into a jump. |
| `pieceElement`, `drawList` (`semantic-table.ts`) | Draw a piece as a control or a `data-tip` span; draw a numbered or bulleted list, keeping the search highlight. |
| `initPieceTooltips` (`tooltips.ts`) | Delegated tooltips over the pieces' `data-tip`, the machinery the graph uses. |

## Key design decisions

### D1 — The projection assigns roles, because it holds the parts

The split cannot be written correctly on the client, because its input is text a writer controls.
The projection composes a summary from a `LineNode`, an `OptionEdge`, and their fragments, so it
knows which characters are whose. This is the tab's usual split: the projection reads, the client
draws.

### D2 — Speech becomes segments by reading the fragments

`SpeechText.Of` flattens speech one way: a `QueryFragment` and a writer's typed `{Key}` both become
`{Key}`. So the projection walks the fragments and marks `query` only for a real
`QueryFragment`. That walk is a second place that knows which fragments contribute words; a
union-coverage test over the fragment kinds guards it, and keeps the published `SpeechText` API
unchanged.

### D3 — The wire carries a role; the client owns the class

The wire says `speaker`, not `dd-sum-speaker`. A role is a fact about the text; a class is how this
client draws it. The mapping lives in `SUMMARY_SEGMENT_CLASS`.

### D4 — The client joins the segments for the cell's text

`SemanticCell.text` — what search matches and sort reads — is the segments joined, computed by the
client. Sending a joined string as well would give two sources that could disagree invisibly.

### D5 — Only three distinctions take a color

Who speaks, what the host is asked to perform, and a value only the game can supply take the Source
editor's token colors (`--tok-speaker-name`, `--tok-command`, `--tok-query`), so a speaker is the
same color in the table as in the script. The table's grammar is muted so it steps back, and the
writer's words keep the cell's plain color, the most legible thing in the row. Keywords and speaker
names sit side by side, so coloring the grammar too would turn a row into a stripe. Every role was
measured against the table background in both themes; the tightest, muted grammar on dark, is
4.77:1.

### D6 — The cap keeps roles

```text
capped(segments):
    budget = 200 characters over the whole joined line, including a leading "IF key? THEN "
    if the line fits, keep every segment
    cut    = the last space within the budget, or the budget when there is none
    keep the first `cut` characters, trailing spaces trimmed, re-sliced across the segments
    append separator("…")
```

Every kept segment keeps its text, role, and target, so a truncated row stays colored and
followable. The cut reads the joined line, because the boundary a reader sees is a property of the
words, not of where a role ends.

### D7 — A list is drawn as the list it is

A `boundary` is a role, not a character, so the client draws it as a break: a menu's options as a
bulleted list (alternatives) and a control's commands as a numbered list (order is meaning). The
cell's text joins the lines with newlines, so search, sort, and copy still read every item and the
highlight lands on the item that holds the match. A branch's `IF`/`THEN`/`ELSE` keeps one line,
because its arms are read together.

### D8 — A piece that names a node carries that node

A branch's `12` and a jump's `⇒ Feel the hallway door` stand for a node, so the projection puts the
node on the piece. The client draws it like any other way out: a control that reveals the node in
the JSON and tints its row on hover.

**The role says what a piece is; the target says where it goes.** A branch number and a jump's
words are `target` pieces, because naming a node is all they do. A menu option and a random arm keep
their own role — `plain`, `query`, `absent` — and carry the target beside it. The Dialogue Graph
draws options and random arms as one kind of route, so both are followable here.

### D9 — What a piece means is the client's to say

`{Gold}` is a query only to a reader who knows the script language. A piece that means something
explains itself on hover — a name, a sentence, and, for a followable piece, what pressing it does —
in the shape the Dialogue Graph uses for its routes. The projection sends only the role (D3).

### D10 — A condition is drawn as the query it is

A condition is the boolean member of the query family: only the running game can answer it. So a
condition key wears the `query` role and the `?` the script marks it with: `IF FoundKey? THEN 12`,
`Brave the west road IF Alice.HasMap?`. A key already ending in `?` reads with two (`Rainy??`), the
same awkwardness the value placeholder accepts.

## Error and boundary cases

| Case | Behavior |
| --- | --- |
| A label holding the option separator (`A \|\| B`) | One `plain` segment. |
| A command argument holding `;` or `,` | One `command` segment for the whole `Name(args)`. |
| A speaker name holding a colon | The whole name is the `speaker` segment. |
| A writer's literal `{hello}` in speech | `plain`, because no `QueryFragment` produced it. |
| A real query inside styled or linked words | Still a `query` segment. |
| A writer typing `<no label>` | Their words: `plain`, never `absent`. |
| A line with no speech | The speaker, then `<no speech>`. |
| A speaker index outside the speaker list | `<unknown>` as the speaker; cannot come from a playbook the writer produced. |
| An option with no label | `<no label>`; the compiler has already reported the blank menu row. |
| A control with one command | No boundary, so it reads as a line. |
| A conditional list, or a draw's header | Its pieces come before the opening boundary, as the introduction. |
| A `target` segment cut by the cap | Keeps its node. |
| An option's condition | A `query` segment with no target: a question about the arm, not a way to it. |
| Two adjacent pieces naming different nodes | Never merged. |
| An unknown node kind | An empty segment list, so an empty cell. |

## Testability

| Level | Covers |
| --- | --- |
| xUnit — `PlaybookNodeSummary` | Per node kind, the segments' text and roles, and the joined line; a writer's separator, semicolon, colon, keyword, and marker each stay one `plain` segment; the stand-ins. |
| xUnit — queries and conditions | A real `QueryFragment` is `query`, inside styled and linked words; literal braces are `plain`; a condition carries `?` as a branch arm, a node prefix, and an option condition. |
| xUnit — the cap | A cut inside a segment, at a boundary, with no space, and with a leading condition. |
| xUnit — boundaries and targets | Opening and between boundaries; none for a lone command; which pieces name which node, including after the cap. |
| xUnit — union coverage | Every node kind yields segments; the speech walk handles every fragment kind. |
| Vitest — `playbook-view` | Role classes; a menu bulleted and a command list numbered with the introduction first; tips; a target is a button carrying the jump and reference key; every role has a class. |
| Vitest — `semantic-table` | List items, numbering, search highlight across items, and a target piece as a control. |
| Playwright | A real playbook's menu, command list, query tooltip, and an arm followed to its node; axe passes. |

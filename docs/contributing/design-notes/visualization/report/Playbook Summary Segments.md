# Playbook Summary Segments

> [!NOTE]
> Status: **implemented**. A node's **summary** travels as labeled **segments** — each piece
> says what it is — so the Nodes table draws what the projection wrote instead of splitting
> one flattened line back apart on delimiters a writer can also type.

## Table of contents

- [Goal and scope](#goal-and-scope)
- [Ubiquitous language](#ubiquitous-language)
- [Functionality checklist](#functionality-checklist)
- [Why a role is needed](#why-a-role-is-needed)
- [Design](#design)
- [Interfaces and abstractions](#interfaces-and-abstractions)
- [Key design decisions](#key-design-decisions)
- [Error and boundary cases](#error-and-boundary-cases)
- [Integration](#integration)
- [Testability](#testability)
- [Open questions](#open-questions)

## Goal and scope

The [Nodes table](./Playbook%20Nodes%20Table.md) colors each node's **summary** in the report's own syntax colors: a
speaker purple, a command olive, a query red. To do that it takes the single string the
projection sent and splits it again, deciding where a writer's words stop and the table's
grammar begins.

That split can be wrong. Every delimiter it looks for — `" || "`, `"; "`, `", "`, `": "`,
`" IF "`, `{…}`, `<…>` — is a character a writer can type, so a label reading
`Go left || right` is read as two options where the script has one, and a writer's literal
`{hello}` is drawn as a query the game will answer.

This component removes the split. The projection composes a summary from parts it already
holds, so it sends those parts as **segments**: a short text and the **role** it plays. The
client maps a role to a class and draws the segment. Nothing is re-parsed, so no writer's words
can be mistaken for the table's grammar.

**In scope:**

- The summary's wire shape: an ordered list of segments on `PlaybookNodeView`.
- The projection that writes the segments, from the typed playbook model — including which
  speech characters are a real query and which the writer merely typed.
- The client that draws each segment by its role.
- One small color change: a **truncated** row keeps the roles of the part it kept, where
  today it is drawn entirely plain (the client could not trust any part of a line it was
  re-parsing). Every other role draws exactly as it does today.

**Out of scope:**

- **Drawing a choice as a list of options.** The segments make the option boundaries findable
  without reading a writer's text — a writer's characters are never a `separator` — so the
  list is its own change built on that, not part of this one.
- **The planned hover card** over a node reference, which will read the same segments.
- The playbook format, its schema, and `SpeechText`'s published flattening — the
  [Speech as Plain Text](../../runtime/Speech%20as%20Plain%20Text.md) contract this note
  consumes.

## Ubiquitous language

One concept, one name — here, in the code, and in both languages.

| Term | Meaning |
| --- | --- |
| **Segment** | A piece of a summary's text together with the role it plays. A summary is an ordered list of segments. |
| **Role** | What a segment is: a speaker, the writer's words, the table's grammar, a command, a query, or an absent marker. |
| **Summary** | The whole line a node reads as — the segments' text, joined end to end. |
| **Grammar** | The table's own words and punctuation: the keywords (`IF`, `THEN`, `ELSE`, `END`, `CONTINUE`, `DRAW 1 FROM`), the separators (colon, `\|\|`, `;`, `⇒`), and the cap's ellipsis. |
| **Absent marker** | A name in angle brackets the report writes where a value-position is empty: `<no speech>`, `<no label>`. |
| **Drawn segment** | The client's own shape for a segment it has mapped — `SemanticSegment`, a text and a class. Distinct from the wire **segment**, which carries a role, not a class. |

"Segment" is chosen over "run" on purpose: the domain already spends "run" on a
playthrough (`DialogueDown.Runtime`, the runner), so a piece of text must not borrow it.
The word names a contiguous piece of a whole, and it collides with none of the compiler's
own words — `span` (source), `fragment` (speech), or `token` (highlighting).

## Functionality checklist

- [x] A node's summary is sent as an ordered list of segments.
- [x] Each segment carries a role the client can draw without reading its text.
- [x] The segments partition the summary: every character belongs to exactly one segment, and
      joining them rebuilds the line.
- [x] A writer's punctuation never changes a role: a label holding an option separator,
      a semicolon, a colon, a keyword, a brace, or a marker is one segment of the writer's
      words.
- [x] A real query is a `query` segment and a writer's literal braces are `plain`, because the
      projection reads the fragments rather than scanning the flattened text.
- [x] Every role draws with the class it draws today, except a truncated row, which now
      keeps the colors of the part it kept.
- [x] The cap still bounds a summary.
- [x] The client no longer splits a summary; `summary-runs.ts` and its tests are deleted.

## Why a role is needed

A choice whose second option is written `Go left || right` cannot be told from a choice with
three options by reading the line: the characters a writer typed are the same ones the table
uses to separate options.

```text
| # | Kind   | Summary                              | Leads to |
| - | ------ | ------------------------------------ | -------- |
| 4 | choice | Take the road \|\| Go left \|\| right | 30, 31   |
```

The same holds at every joint — a command argument holding a semicolon, a speaker name
holding a colon, a label holding a keyword, an option written `<no label>` — which is why the
projection labels each piece instead of the client reading the line back.

## Design

```mermaid
flowchart LR
    P["PlaybookDocument<br/>(typed nodes)"] --> J["PlaybookProjection"]
    J --> R["PlaybookNodeSummary<br/>writes an ordered list of segments"]
    F["SpeechFragment list<br/>(line speakers use)"] --> R
    R --> V["PlaybookNodeView<br/>one segment list per node"]
    V --> SC["summaryCell()<br/>maps each role to its class"]
    SC --> T["nodeTable()"]
```

The projection already holds every part as it builds the line; today it throws the shape
away by joining them into a string. The change is to keep the shape.

### Roles

A segment's role is the class the client draws it with. The projection assigns each role where
it knows the part's job; nothing is inferred from characters.

| Role | Assigned to | Class today |
| --- | --- | --- |
| `speaker` | A line's speaker name, including the `<anonymous>` and `<unknown>` stand-ins, which stand where a name would | `.dd-sum-speaker` |
| `plain` | The writer's words: speech minus its queries, an option's label, a divert's words, a condition key, a branch target | none (the cell's own color) |
| `keyword` | The table's words: `IF`, `THEN`, `ELSE`, `END`, `CONTINUE`, `DRAW 1 FROM n` | `.dd-sum-keyword` |
| `separator` | The table's punctuation: colon, `\|\|`, `;`, `⇒`, and the cap's ellipsis | `.dd-sum-separator` |
| `command` | A command in round brackets: `(fade in)`, `ShowBackground(tavern, firelit)` | `.dd-sum-command` |
| `query` | A `{Key}` written for a real `QueryFragment` in speech or odds | `.dd-sum-query` |
| `absent` | A value-position marker the report wrote: `<no speech>`, `<no label>` | `.dd-sum-absent` |

The set is exactly today's `SummaryRole` union, so `SUMMARY_SEGMENT_CLASS` and `styles.css`
keep their selectors.

The roles stay coarse on purpose. Naming an option or a condition apart from the writer's
words would push structure into the summary, rebuilding the dialogue graph's shape inside a
payload whose job is to be read as one line. A client that wants the complete structure should
read the playbook itself; the summary stays a summary, and a future list rendering can consume
a `separator` segment as its item boundary instead of drawing it.

### Writing a summary as segments

Each node kind composes its segments the way it composes its string today, one part at a
time. A part that is the writer's words becomes the writer's whole text; the stand-ins are
segments of their own.

```text
runsOf(node, speakers):
    line          -> speaker(nameOf(line.speaker)) ":" speechSegments(line.speech)
    control       -> commands, if any -> command(", " between each)
                     otherwise        -> "⇒" divertWords | keyword("CONTINUE")
    choice        -> option, one per option, "||" between
    random-choice -> keyword("DRAW 1 FROM n") ":" odds per arm, "||" between
    branch        -> keyword("IF") key keyword("THEN") target, and so on
    end           -> keyword("END")

    # A node-level condition leads, exactly as the string leads today:
    keyword("IF") key keyword("THEN") body

    nameOf(index) -> the speaker's name; absent("<anonymous>") when unnamed;
                     absent("<unknown>") when the index is outside the list
    option        -> plain(label) | absent("<no label>"), plus keyword("IF") key
                     when the option is guarded

speechSegments(fragments):
    TextFragment        -> plain(text)
    QueryFragment       -> query(placeholder)
    StyledTextFragment  -> speechSegments(children)
    LinkFragment        -> speechSegments(label)
    ImageFragment       -> speechSegments(alt)
    LineBreakFragment   -> plain(" ")
    anything else       -> nothing          # the same fragments SpeechText drops

    # Adjacent plain segments merge, so a styled span's words read as one segment.
```

## Interfaces and abstractions

| Type | Responsibility | Collaborators |
| --- | --- | --- |
| `PlaybookSegmentView(string Text, string Role)` | One segment: a text and the role it plays, with a factory per role. Its own file. | `PlaybookNodeSummary`, `SummaryRoles` |
| `SummaryRoles` | The role names the wire carries, kept apart from the writing that uses them. | `PlaybookSegmentView` |
| `PlaybookNodeView(…, Segments)` | One node as the table shows it; gains the segments. | `PlaybookProjection` |
| `PlaybookNodeSummary.SegmentsOf(node, speakers)` | Writes a node's segments. Replaces the string-returning `Of`. | the playbook's node and edge models, `SpeechText.PlaceholderFor` |
| `PlaybookProjection.ToView` | Projects each node's segments. | `PlaybookNodeSummary` |
| `model.ts` — `PlaybookSegmentView`, `SummaryRole` | The wire shape and the role union in TypeScript. Both move here. | `playbook-view.ts` |
| `summaryCell(node)` | Joins the segments for the cell's text and maps each role to its class. | `SUMMARY_SEGMENT_CLASS` |
| ~~`summary-runs.ts`~~ | **Deleted**: the grammar it re-parsed no longer exists on the wire. | — |

## Key design decisions

### DD1 — The projection assigns the roles, because it holds the parts

The split is not hard to *write*; it is impossible to write *correctly*, because the input
is text a writer controls. Moving it to the projection is not a performance change — it is
the only place the answer exists. The projection composes a summary from a `LineNode`, an
`OptionEdge`, and their fragments, so it knows which characters are the writer's and which
are its own; the client has only the finished line.

This is the same split the speaker and anchor tables already follow — the projection reads,
the client draws — and the same reason the planned hover card can show identical words
without a second implementation.

### DD2 — Speech becomes segments by reading the fragments, not by scanning its text

`SpeechText.Of` flattens a fragment list to one string, and by its own remarks that reading
is lossy and one-way: a `QueryFragment` becomes `{Key}`, but so do the characters `{`,
`Key`, and `}` when a writer types them. No scan of the flattened string can tell a real
query from typed braces — the issue lists literal braces as a trip — so the projection
walks the fragments instead and marks `query` only for a real `QueryFragment`.

Two ways to get that walk:

| Option | How | Tradeoff |
| --- | --- | --- |
| **Walk in the projection** (this note's choice) | `PlaybookNodeSummary` recurses over `SpeechFragment`: text and breaks to `plain`, a query to `query`, styled, link, and image by recursing, anything else dropped — the same ones `SpeechText` drops. | A second place knows which fragments contribute words, so a new fragment kind could be missed. A union-coverage test over the fragment kinds guards it, the pattern this repo already uses for node kinds. The published format library stays untouched. |
| **A seam on `SpeechText`** | The format library gains a public API that yields plain and query slices, and the string and the segments both come from it. | One source of truth for the flattening, at the cost of public API on a published library for one report's benefit. |

The walk is this note's choice: the guard is cheap, and it leaves the published library
untouched. The seam is worth revisiting only if a second consumer wants slices too.

### DD3 — The projection assigns a role; the client owns the class

The wire says `speaker`, not `dd-sum-speaker`. The role is a fact about the text; the class
is how this client happens to draw it. Keeping the class name in the payload would put a
CSS detail in the report's data and break the day a role is restyled. The mapping stays in
`SUMMARY_SEGMENT_CLASS`, where it is today.

### DD4 — The wire carries segments, and the client joins them for the cell's text

The row's cell needs one plain string: `SemanticCell.text`, which search matches and which
the search highlight is laid across the segments from. The tempting shape is to send that
string beside the segments, but the two can then disagree, and a disagreement is invisible —
the cell draws one text and highlights against another. Since the client is the only
consumer, it joins the segments itself:

```text
summaryCell(node):
    segments = node.segments.map(toDrawnSegment)
    text = segments.map(segment => segment.text).join("")
```

`segments` then concatenates to `text` by construction, which is exactly the invariant
`SemanticCell` already documents. No `summary` field, no second source of truth, and no
possible drift. (This differs from the issue's draft, which kept a joined `Summary`; the
reason to drop it is that the client can join, so the field can only add a way to be
wrong.)

### DD5 — The cap no longer degrades a truncated row to plain

Today the client draws a truncated summary as one plain segment: it cannot tell whether the cut
fell inside a command, a query, or the writer's words. With roles, it can — every segment before
the cut is known. So the projection keeps the roles of the kept part and marks the ellipsis
as a separator. The row is both bounded and still colored. This is the one intended color
change.

The algorithm, which is the fiddliest part of the change:

```text
capped(segments):
    budget = 200 characters, counted over the whole line — including the leading
             "IF key THEN " when the node carries a condition, as the line counts it today
    line = the segments' text joined
    if the line fits the budget, keep every segment whole
    cut = the last space in the line within the budget, or the budget when it holds none
    keep = the line's first `cut` characters, with trailing spaces trimmed
    re-slice the segments to `keep`, then append separator("…")
    every kept segment keeps its text and its role, so the partition still holds
```

The cut reads the joined line rather than walking the segments, because the boundary a reader sees
is a property of the words, not of where one role happens to end.

### DD6 — Delete the splitter rather than keep it as a fallback

A fallback splitter would be dead code the day it stopped being reachable, and terrifying
the day it became reachable again. The client's input is now always segments, so the correct
number of splitters is zero. (A missing `segments` would draw an empty cell — a visible
failure, not a silent re-parse; the payload and the client are built from the same source,
so a mismatch is a development-time accident, not a shipped one.)

## Error and boundary cases

| Case | Behavior |
| --- | --- |
| A label holding the option separator (`A \|\| B`) | One `plain` segment; the writer's separator is never a `separator` segment. |
| A command argument holding a semicolon or comma | One `command` segment for the whole `Name(args)`; the argument is not split. |
| A speaker name holding a colon | The whole name is the `speaker` segment; the divider is the one the projection placed. |
| A writer's literal braces in speech (`{hello}`, no query) | One `plain` segment: the projection marks `query` only for a real `QueryFragment`. |
| A real query in speech or odds (`{Gold}`) | A `query` segment, wherever it sits, including inside styled or linked words. |
| `<no speech>` or `<no label>` the projection wrote | An `absent` segment in the value position. |
| The same characters typed by a writer | Their words: a `plain` segment, never `absent`. |
| A line whose speaker index is outside the speaker list | The `<unknown>` stand-in as a `speaker` segment, as today. |
| A choice, random choice, or branch truncated by the cap | Segments before the cut keep their roles; a `separator` ellipsis ends the line. |
| A node kind with no summary (an unknown kind) | An empty segment list; the cell draws an empty text. |

## Integration

- **`PlaybookReport.cs`** — `PlaybookNodeView` gains `Segments`. `PlaybookSegmentView` and
  `SummaryRoles` are files of their own, so a reader finds the shape and the vocabulary without
  scrolling the writer that uses them.
- **`PlaybookNodeSummary.cs`** — `Of` is replaced by `SegmentsOf`, which returns the pieces; the
  speech walk and the cap live here. No string-returning form survives, because the cell's text is
  its pieces joined.
- **`PlaybookProjection.cs`** — passes `SegmentsOf(...)` into the view.
- **`model.ts`** — gains `PlaybookSegmentView` and the `SummaryRole` union, and `segments` on
  `PlaybookNodeView`.
- **`playbook-view.ts`** — `summaryCell` joins the segments for `text` and maps each role to its
  class; `SUMMARY_SEGMENT_CLASS` stays, keyed by the union now in `model.ts`.
- **`model.ts`** / **`semantic-table.ts`** — the drawn shape is renamed with the concept:
  `SemanticSegment` replaces `SemanticRun`, and a cell carries `segments`, so "run"
  leaves the feature's vocabulary.
- **`summary-runs.ts`**, **`summary-runs.test.ts`** — deleted.
- **`styles.css`** — unchanged; every class keeps its selector.
- **`web/dist/report.html`** — rebuilt and committed, as any `web/src` change requires.
- **No format change.** The playbook document and its schema are untouched; only the
  report's own payload changes shape.

## Testability

| Level | Covers |
| --- | --- |
| xUnit — `PlaybookNodeSummary` | One test per node kind, asserting the segments' text *and* roles; a writer's separator, semicolon, colon, keyword, and marker each stay one `plain` segment; the stand-ins; the cap. |
| xUnit — queries | A real `QueryFragment` is a `query` segment, including inside styled and linked words; a writer's literal `{hello}` is `plain`. |
| xUnit — the line | A node's pieces join to the exact pseudocode line the table shows, asserted per kind, so the joined text is pinned where the pieces are built. |
| xUnit — the cap | A cut inside the crossing segment, a cut at a segment boundary, a crossing segment with no space, and the leading condition counting against the budget. |
| xUnit — union coverage | Reflecting over the node union, every registered kind yields segments; and over the fragment union, the speech walk handles every kind. |
| Vitest — `playbook-view` | A cell joins its segments to its text and draws each role in its class; a label holding a separator stays one plain segment; every role in `SummaryRole` is a key of `SUMMARY_SEGMENT_CLASS`. |
| Playwright | The Nodes table renders for a real playbook, its summary cells included, and the tab keeps its accessibility checks green. |

Moving the cases off `summary-runs.test.ts` and onto `PlaybookNodeSummaryTests` is the
point: they are asserted against real nodes and fragments rather than against strings the
test itself composed.

## Open questions

None outstanding. The wire carries the pieces alone — the client joins them for the cell's
text — and the walk over the speech fragments lives in the projection, guarded by a
union-coverage test.

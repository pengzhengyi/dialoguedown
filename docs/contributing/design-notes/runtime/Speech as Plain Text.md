# Speech as Plain Text

> [!NOTE]
> Status: **proposed** — not yet implemented. A line's speech is a list of
> fragments, because a host renders it and every host renders it differently.
> But several places need the same lossy fallback: the words, as one line of
> plain text. Four of them already compute it, three different ways, and two of
> those are wrong — one of them on every report surface that shows a line. This
> note designs the single public function they should share, and repairs the
> compiler-side flattening the report reads through.

## Table of contents

- [Goal and scope](#goal-and-scope)
- [Ubiquitous language](#ubiquitous-language)
- [What exists today](#what-exists-today)
- [Functionality checklist](#functionality-checklist)
- [The flattening](#the-flattening)
- [Interfaces and abstractions](#interfaces-and-abstractions)
- [Key design decisions](#key-design-decisions)
- [Error and boundary cases](#error-and-boundary-cases)
- [Integration](#integration)
- [Testability](#testability)
- [Open questions](#open-questions)

## Goal and scope

A playbook holds prose in two places — a line's `speech` and an option's `label` —
and both are lists of fragments rather than strings. That is deliberate: a host
renders them, and Godot, a browser, and a terminal each render them differently.

Some consumers do not render, though. They need the words. A conformance fixture
asserts what was said without caring that one word was bold. A report table shows
a node's line in a cell. A log line names what a runner emitted. For all of them
the answer is the same string, and the conformance corpus already specifies that
string normatively, because a port's conformance depends on producing it.

This component is that specification, implemented once as a public function.

**In scope:**

- `SpeechText.Of(speech)` — public, in `DialogueDown.Playbook`: a fragment list in,
  one line of plain text out.
- A defined contribution for each of the nine fragment kinds.
- Resolving a query fragment, for callers that can answer one, and a documented
  placeholder for callers that cannot.
- A `Query` case in the compiler's `InlineText.Of`, so the report stops dropping
  queries from the words it draws.

**Out of scope:**

- **Rendering with styling.** BBCode, HTML, and terminal markup are a separate,
  surveyed concern with its own note. Plain text is the one rendering that throws
  styling away, which is why it can be a function rather than a framework.
- **Parsing plain text back into fragments.** The flattening is lossy by design and
  has no inverse.
- **Any change to the playbook format, its schema, or the serialized document.**
  This adds a way to read what is already there.
- **Migrating the runtime's own test helper.** It lives on a branch this work does
  not touch, and it adopts this function after this lands. See
  [What exists today](#what-exists-today).

## Ubiquitous language

| Term            | Meaning                                                                                                                        |
| --------------- | ------------------------------------------------------------------------------------------------------------------------------ |
| **Fragment**    | One piece of what a line says — text, a styled run, a link, an image, a break, a query, a tag, or a command. Nine kinds.       |
| **Flattening**  | Turning a fragment list into one line of plain text. The conformance corpus already uses this word for exactly this operation. |
| **Plain text**  | What a flattening yields: the words, without styling, nesting, or markup.                                                      |
| **Query**       | A fragment that asks the world for a value and says the answer. The only fragment whose plain text depends on something else.  |
| **Answer**      | What a caller supplies for a query key.                                                                                        |
| **Placeholder** | What stands in for a query a caller cannot answer: the key in braces, `{HeroName}`.                                            |

*Flattening* and *plain text* are the repository's existing words, taken from the
corpus note and from the helper the compiler already has. This note invents no
vocabulary.

## What exists today

Four places answer this question already, written independently of each other.

| Where                                  | What it does                                                                                                                           | Status                         |
| -------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------ |
| `Conformance Corpus.md`                | Specifies it normatively for every port: "concatenate each fragment's plain text, drop style markers, and substitute resolved queries" | The contract. No code.         |
| `script/ast/InlineText.cs`             | Flattens the compiler's AST fragments. Text, styled children, link label, image alt, a break as a space, everything else empty.        | **Drops queries** — see below. |
| The report's eight label sites         | Call `InlineText.Of` to draw a line, a divert, an option, a scene heading.                                                             | Callers, not implementations.  |
| The runtime's `StepAssert` test helper | Concatenates only the top-level text fragments.                                                                                        | **Drops nested styling.**      |

Both defects are the same mistake in different clothing: a flattening that handles
the fragment kinds its author happened to need and silently contributes nothing for
the rest. Neither fails loudly. Both produce a plausible string that is missing
words.

**Queries vanish from the report.** A query is a game call, and `InlineText.Of`
ends in a catch-all that yields the empty string, so it drops one. That flattening
feeds eight label sites across the Dialogue Graph, the Semantic Model, and the
Desugared AST tab — every surface that draws a line's words. On a shipped example
script, a line written

```text
Rain hammers the shutters of the Salted Hart. You are `"HeroName"`, and your purse
holds `"Gold"` gold — enough for a bed, not for a legend.
```

is drawn in the Dialogue Graph as

```text
Rain hammers the shutters of the Salted Hart. You are , and your purse holds  gold — enough for a bed, not for a legend.
```

This is live, and it is on the surfaces a writer reads. Fixing it is in scope,
because a shared flattening that renders a query while the compiler's drops one
would be two helpers disagreeing about the one fragment kind this note is mostly
about — exactly the drift DD3 exists to prevent.

**Nested styling vanishes from the runtime's test helper.** Taking only top-level
text fragments means the corpus note's own example —

```json
[ { "kind": "text", "text": "My key is " },
  { "kind": "styled", "style": "bold", "children": [ { "kind": "text", "text": "rusty" } ] },
  { "kind": "text", "text": "." } ]
```

— flattens to `"My key is ."` rather than `"My key is rusty."`. No runtime test uses
a styled fragment yet, so this one is still latent. It stops being latent the moment
a conformance fixture about conditions happens to contain a bold word, which the
corpus explicitly invites: a fixture writes the string form precisely when styling
is *not* its subject.

That helper lives on in-flight runtime work rather than on the main line, so this
component does not reach in and change it. Landing the shared function first is what
makes the fix available: the playbook library is already referenced there, so
adoption becomes a one-call-site change owned by the branch that needs it. The same
holds for the conformance harness, which has not implemented speech matching yet, so
the shared function can land before the matcher that would otherwise grow a fifth
flattening.

## Functionality checklist

- [ ] Every one of the nine fragment kinds has a defined plain text.
- [ ] Styled runs contribute their words and drop their style.
- [ ] Nesting is followed to any depth.
- [ ] A link contributes its label; an image contributes its alt text.
- [ ] A line break contributes a single space, because the result is one line.
- [ ] A tag and a command contribute nothing — they are not words in the line.
- [ ] A caller that can answer a query gets the answer substituted.
- [ ] A caller with no answers gets the key in braces, `{HeroName}`.
- [ ] An option's label flattens by the same function as a line's speech.
- [ ] The result is returned exactly as composed; trimming is the caller's business.
- [ ] `InlineText.Of` renders a query the same way, so the report stops drawing a
      line with its queries missing.
- [ ] The placeholder's rough edges are documented where a writer will meet them.

## The flattening

| Fragment          | Plain text                | Why                                                      |
| ----------------- | ------------------------- | -------------------------------------------------------- |
| `text`            | its `text`                | The words themselves.                                    |
| `styled`          | its `children`, flattened | The corpus says to drop style markers, not styled words. |
| `link`            | its `label`, flattened    | The label is what a reader sees; the target is not said. |
| `image`           | its `alt`, flattened      | Alt text exists to stand in for the image in words.      |
| `break`           | a single space            | The result is one line, so a line break becomes a gap.   |
| `query`           | the answer, else `{Key}`  | The corpus says to substitute resolved queries.          |
| `tag`             | nothing                   | Metadata about the line, not words in it.                |
| `default-command` | nothing                   | An effect the host performs, not something said.         |
| `custom-command`  | nothing                   | As above.                                                |

```mermaid
flowchart LR
    F["SpeechFragment[]"] --> T["SpeechText.Of"]
    A["answer a query<br/>(optional)"] --> T
    T --> S["one line of plain text"]
    S --> C1["conformance matcher"]
    S --> C2["Playbook Nodes Table"]
    S --> C3["a host's fallback rendering"]
```

## Interfaces and abstractions

| Type                                                                   | Responsibility                                                                                       | Collaborators                 |
| ---------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------- | ----------------------------- |
| `SpeechText`                                                           | Public static class in `DialogueDown.Playbook.Speech`. The flattening, and nothing else.             | the nine fragment records     |
| `SpeechText.Of(ImmutableArray<SpeechFragment>)`                        | Flattens, rendering a query as a placeholder.                                                        | —                             |
| `SpeechText.Of(ImmutableArray<SpeechFragment>, Func<string, string?>)` | Flattens, asking the caller to answer each query key. A `null` answer falls back to the placeholder. | the caller's world or fixture |

## Key design decisions

### DD1 — It lives in the playbook library, public

The playbook library is the contract between a compiler and a runtime, and its
project file says what that means: a game embeds this and a runner, never the
compiler. Rendering happens at play time, from what the runner emits, so a
fallback rendering has to be reachable from there.

The three callers settle it between them. A conformance matcher lives in the
runtime's tests, the Nodes table lives in the visualization assembly, and a host's
fallback lives in somebody else's game. The only assembly all three already
reference is this one.

### DD2 — A function now, the formatter seam later

A surveyed sibling note proposes an `ISpeechFormatter` seam with one `Format`
method per target — BBCode for Godot, markup for a terminal, HTML for the web —
and a shared visitor walking the fragment tree while each formatter fills in
`OpenStyle` / `CloseStyle` / `EscapeText` hooks. Plain text is clearly one of those
targets, so the question is whether to build the seam now and make plain text its
first implementation.

The recommendation is **no, not yet** — and the reason is not cost, it is fit.
Plain text is the **degenerate** formatter: it is the one target that needs no
`OpenStyle`, no `CloseStyle`, and no escaping, because it throws all of that away.
Designing the shared visitor against it would shape the abstraction around the one
case that exercises none of it, and the first real formatter would then be a
rewrite rather than an addition. An interface extracted from two implementations
fits both; an interface extracted from the empty case fits neither.

Nothing is lost by waiting. A static `Of` can be wrapped by an implementation of
the seam whenever the seam arrives, without touching a single caller.

This is a judgment call about sequencing rather than about whether the abstraction
is worth having, so it is worth disagreeing with if the seam feels closer than it
looks from here.

### DD3 — Two implementations coexist, and that is not duplication

The compiler already has `InlineText.Of`, and it is correct. It is also on a
different type: the compiler's `InlineFragment`, which is internal to the compiler
assembly and which a game never sees. The playbook's `SpeechFragment` is the public
type a runtime holds.

Neither can be expressed in terms of the other. They are different types in
different assemblies with different lifetimes — `InlineText` runs during a compile,
on scene headings and jump labels, before a playbook exists at all; `SpeechText`
runs after, on what was written. Collapsing them would mean either making the
compiler's AST public or making the playbook depend on the compiler, and the
project is deliberately arranged so that neither is true.

What they must not do is disagree. A test pairs corresponding fragments of the two
types and asserts both helpers produce the same string, so a change to one that the
other does not follow fails a build rather than a port's conformance run.

That test is what pulls the compiler's query bug into this component. `SpeechText`
renders a query; `InlineText` drops one; the two cannot both be right. Rather than
exempt the one fragment kind this note is mostly about, `InlineText.Of` gains the
same `Query` case — one arm before its catch-all, which leaves commands and tags
contributing nothing as they do today. No existing test flattens a query through it,
so nothing else moves.

### DD4 — A query is answered by the caller, not by the function

A query is the one fragment whose plain text is not in the fragment. The corpus
says to substitute **resolved** queries, which is unambiguous there because a
fixture supplies its own answers — and undefined for a caller that has none.

The three callers genuinely differ:

| Caller              | Answers available                   | Wants                        |
| ------------------- | ----------------------------------- | ---------------------------- |
| Conformance matcher | The fixture's own answers           | The substituted value        |
| The report          | None — a static report has no world | Something that names the key |
| A host's fallback   | The live world                      | The substituted value        |

So the function takes the answers rather than owning them, and the no-answer case
is a documented placeholder rather than a guess or a throw. A throw would make a
report crash on a script that is perfectly valid. An empty string is what the
report does today, and it is the defect this note repairs.

### DD5 — An unanswered query reads as `{Key}`

The placeholder is `{HeroName}`: the key, in braces. It is a rendering convention,
not DialogueDown syntax, and it is deliberately not written the way a writer writes
a query.

That looks like it breaks a good rule — do not make a writer learn a second
notation for something the language already spells. The reason it does not is that
**the placeholder says a different thing than the source does.** A writer's
`` `"HeroName"` `` says *a query is written here*, which the writer already knows,
having written it. The placeholder says *a value goes here, and this surface cannot
know it yet — the runtime resolves it later.* That second fact is the one worth
drawing, because it is exactly what differs between the script and what a player
will see. It shows a writer which parts of a line are dynamic.

Echoing the source back cannot say that. It also cannot be written honestly. The
writer's notation for a query is the whole code span, backticks included — the
backticks are what mark the content as a game construct rather than prose. So the
candidates were:

| Rendering                                                    | Why not                                                                                                                                         |
| ------------------------------------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------- |
| `You are "HeroName", and your purse holds "Gold" gold`       | Not the writer's notation, only the inside of it; and the quotes collide with the quotes a report puts around speech.                           |
| ``You are `"HeroName"`, and your purse holds `"Gold"` gold`` | Shows Markdown delimiters as content. In a table cell or an SVG label there is no code-span rendering, so the backticks are literal characters. |
| `You are HeroName, and your purse holds Gold gold`           | Reads as though the line says the word "HeroName".                                                                                              |

Brace-wrapping also follows a precedent this project already set. The Dialogue
Graph draws a jump as `⇒` rather than the `=>` a writer types, and says why: a
drawing shows the meaning and leaves the source characters to the source. A query
is the same question with the same answer.

The rough edges are real, and are documented rather than designed away:

- A writer who sees `{HeroName}` and types it into a script gets literal text and no
  diagnostic. Braces mean nothing in DialogueDown, and teaching them to would be a
  language change for a rendering detail.
- A writer whose prose genuinely contains a brace produces a summary that cannot be
  told apart from a resolved query.
- A query key may contain any character except a double quote, so a key holding a
  closing brace nests confusingly. Quotes would be provably unambiguous here, since
  a key cannot contain one — but that trade buys safety in a case no real script
  reaches, at the cost of the collision above in the common one.

### DD6 — Nothing is trimmed, and nothing is capped

`Of` returns exactly what the fragments compose. A line's speech often has
meaningful leading or trailing space — the corpus example's first fragment ends in
one — and a function that trimmed would make round-tripping through it lossy in a
second, undocumented way.

Callers trim when they want to: the Dialogue Graph's label already does, and the
Nodes table's cap belongs to the table. This follows what `InlineText.Of` already
established.

### DD7 — Commands and tags are not words

A tag is metadata a host reads for a portrait or a voice; a command is an effect a
host performs. Neither is something a speaker says, so neither contributes plain
text — which is also what `InlineText.Of` does today with its catch-all, and what
the corpus implies by calling the result the line's plain text.

The consequence worth stating: a line that is *only* a command flattens to the
empty string. That is correct, and a caller that wants to say something about such
a line has to say it itself. The Nodes table does exactly this, describing a
control node by its effects rather than by its speech.

## Error and boundary cases

| Case                                        | Behavior                                                                                                                                  |
| ------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| An empty fragment list                      | The empty string. A line with no speech is a line that says nothing.                                                                      |
| A styled run with no children               | Contributes nothing.                                                                                                                      |
| Nesting many levels deep                    | Followed to the bottom; the recursion is over a tree the reader already validated.                                                        |
| A link or image whose label or alt is empty | Contributes nothing, rather than the target or source URL. A reader asked for words, and a path is not words.                             |
| A query whose answer is `null`              | The placeholder, as though no resolver were supplied. A resolver that cannot answer one key still answers the others.                     |
| A query whose answer is the empty string    | The empty string. An answered query is answered, even when the answer is nothing.                                                         |
| A fragment kind added to the format later   | Contributes nothing, via the catch-all the match needs anyway — and fails the coverage test described below, so it cannot ship unnoticed. |
| A line that is only commands and tags       | The empty string.                                                                                                                         |

## Integration

- **`speech/SpeechText.cs`** — new, public, in `DialogueDown.Playbook`.
- **`script/ast/InlineText.cs`** — gains a `Query` arm before its catch-all, which
  stops the report drawing a line with its queries missing. Kept rather than merged
  away; see DD3.
- **The report's label sites** — unchanged code, changed output: eight places across
  the Dialogue Graph, the Semantic Model, and the Desugared AST tab begin drawing a
  query instead of swallowing it. Any snapshot or label assertion over a script with
  a query needs its expectation updated, which is the bug being fixed rather than a
  regression.
- **`PlaybookNodeSummary`** — the Nodes table's summary consumes it, for a line's
  speech and for an option's label alike.
- **The conformance harness** — its speech matcher, still to be written, compares a
  fixture's string form against this function, so the normative flattening and the
  implementation are the same thing by construction.
- **The runtime's `StepAssert`** — not touched here. It adopts this function when the
  in-flight runtime work next rebases, which retires its own flattening and the
  nested-styling bug with it.
- **The writer's guide** — the queries section of the game-state guide gains a short
  passage on the placeholder: that a surface with no world shows `{Key}`, that braces
  are a drawing convention rather than script syntax, and that typing them into a
  script writes literal braces. This is where a writer already goes to learn what a
  query is, so it is where they should meet what one looks like unresolved.
- **No format change.** Nothing about the serialized document moves.

The public surface grows by one static class, which is worth noting because the
library's published API is meant to stay small and deliberate.

## Testability

| Level                               | Covers                                                                                                                                                                                                                                                                                           |
| ----------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| xUnit — one test per kind           | The nine rows of the flattening table, each asserted on its own.                                                                                                                                                                                                                                 |
| xUnit — nesting                     | Styling inside styling; a link whose label is styled; the corpus note's own example, asserted to produce the string the corpus prints.                                                                                                                                                           |
| xUnit — queries                     | Substituted from a resolver; `{Key}` with no resolver; a `null` answer; an empty answer; several keys where only some resolve.                                                                                                                                                                   |
| xUnit — coverage                    | Reflecting over the fragment union's registered members, every kind is handled deliberately rather than reaching the catch-all. The repository's `UnionAssert` already asserts union completeness this way.                                                                                      |
| xUnit — agreement with `InlineText` | Corresponding fragment pairs of the two types flatten to the same string, queries included — the assertion that keeps the compiler's helper and this one from drifting apart.                                                                                                                    |
| Generative (optional)               | Flattening never throws and never returns `null`, over randomly generated fragment trees. Worth little for a total function on a closed union, and this test project does not reference Bogus today, though two sibling projects do. Listed so the decision is deliberate rather than forgotten. |

Every test is a pure function call on hand-built fragments: no document, no
compile, no playbook.

## Open questions

- **Should the report mark an unanswered query as a diagnostic rather than only
  drawing it?** A line whose words depend on the world is worth knowing about when
  auditing a script, and the report has a Problems panel that could say so. Against:
  a query is perfectly valid and enormously common, so a diagnostic per query would
  bury the panel in noise. Probably the answer is no, but the question belongs to
  whoever next works on reader diagnostics rather than to this function.
- **Should `Of` take `IEnumerable<SpeechFragment>` instead of
  `ImmutableArray<SpeechFragment>`?** The model exposes immutable arrays, so the
  concrete type matches every real call and avoids an allocation. A broader
  parameter would accept a `Where` over fragments without a round trip through an
  array, which a host writing its own partial rendering might want.

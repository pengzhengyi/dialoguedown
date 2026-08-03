# Block controls

> [!NOTE]
> Status: **implemented**. Runtime evaluation is deferred to the graph/runtime
> ([#45](https://github.com/pengzhengyi/dialoguedown/issues/45)).

## Table of contents

- [Block controls](#block-controls)
  - [Goal and scope](#goal-and-scope)
  - [Functionality checklist](#functionality-checklist)
  - [Ubiquitous language](#ubiquitous-language)
  - [Prior art](#prior-art)
  - [Chosen shape](#chosen-shape)
    - [Marker spelling — two spans](#marker-spelling--two-spans)
    - [Grouping — one connected blockquote](#grouping--one-connected-blockquote)
    - [Rendering in a stock preview](#rendering-in-a-stock-preview)
  - [Writer-facing behavior](#writer-facing-behavior)
  - [Grammar](#grammar)
  - [Architecture](#architecture)
  - [Interfaces and responsibilities](#interfaces-and-responsibilities)
  - [Key design decisions](#key-design-decisions)
    - [D1 — Recognize in the transpiler, from the blockquote structure](#d1--recognize-in-the-transpiler-from-the-blockquote-structure)
    - [D2 — The marker is the two-span form](#d2--the-marker-is-the-two-span-form)
    - [D3 — One connected blockquote, enforced; a severed chain is an error](#d3--one-connected-blockquote-enforced-a-severed-chain-is-an-error)
    - [D4 — `ControlBlock` / `Branch` mirror `Choices` / `Choice`](#d4--controlblock--branch-mirror-choices--choice)
    - [D5 — Editor support offsets the `>` authoring tax](#d5--editor-support-offsets-the--authoring-tax)
    - [D6 — A scene is a top-level unit; no heading inside a branch](#d6--a-scene-is-a-top-level-unit-no-heading-inside-a-branch)
    - [D7 — Separate every marker and utterance with a quoted blank line](#d7--separate-every-marker-and-utterance-with-a-quoted-blank-line)
  - [Markdown interaction](#markdown-interaction)
  - [Diagnostics](#diagnostics)
  - [Error and boundary cases](#error-and-boundary-cases)
  - [Testability](#testability)
  - [Alternatives not chosen](#alternatives-not-chosen)
  - [Crosscheck](#crosscheck)
  - [Open questions and deferred work](#open-questions-and-deferred-work)

## Goal and scope

A writer often wants a **group** of dialogue — several lines, a choice, a jump — to
play only under some game-state condition, and to fall back otherwise. The inline
**condition** (`` `key?` ``) already guards a single
[jump](./Conditional%20Jump.md), [line](./Conditional%20Line.md), or
[choice option](./Conditional%20Choice.md), each **independently** and with **no
`else`**. This construct is the complementary one: a **block `if`/`elseif`/`else`** —
**grouped, mutually-exclusive** branches with an optional fallback.

In scope: the surface **shape** (marker spelling and grouping), the **grammar**, the
**AST** and how it is recognized while transpiling, **diagnostics**, and source
**spans**.

Out of scope: **runtime evaluation** — choosing and playing a branch belongs to the
graph/runtime ([#45](https://github.com/pengzhengyi/dialoguedown/issues/45)) — and
**negation / in-script expressions**, unchanged from the condition primitive (a
writer composes logic behind a single game-defined key).

This note assumes the shipped [Unquoted Keys](./Unquoted%20Keys.md) and
[Control Line](./Control%20Line.md) notes and does not repeat them: a **condition** is
unquoted by default, and a branch **marker** is a *control line* (an effect-only,
speaker-less block). Read those, plus [Progression Order](./Progression%20Order.md),
first.

## Functionality checklist

Design targets for the implementation:

- [x] Recognize a **connected blockquote** led by an `` `if` `` marker as a single
      `ControlBlock` with mutually-exclusive branches.
- [x] Parse the **two-span marker** — `` `if` ``/`` `elseif` `` plus the verbatim
      condition span `` `cond?` ``, and a bare `` `else` `` — reusing the shipped
      condition reader.
- [x] Group each **branch body** across blank-line-separated utterances, bounded by
      the blockquote (no terminator keyword).
- [x] Nest via a nested `> >` blockquote, recursively.
- [x] Report a **severed chain** (a `` `elseif` ``/`` `else` `` opening its own
      blockquote) and **malformed marker order** as errors; never silently re-pair.
- [x] Report a **scene heading inside a branch** — a scene stays a top-level unit.
- [x] Require a **quoted blank line** between markers/utterances and to close a nested
      branch; report a marker fused into content.
- [x] Read a **non-marker blockquote** as a transparent wrapper — its inner blocks in place.
- [x] Preserve source spans of the block, each branch, and each condition.
- [x] Handle `ControlBlock` in every block switch (rewriter, traversal, projection,
      validation), with focused tests at each seam.

## Ubiquitous language

| Term | Meaning |
| --- | --- |
| **Condition** | The shipped game-state boolean, `` `key?` `` (see [Unquoted Keys](./Unquoted%20Keys.md)). |
| **Marker** | The token that opens a branch: `` `if` `` / `` `elseif` `` (each with a condition) or `` `else` ``. A *control line* (see [Control Line](./Control%20Line.md)). |
| **Branch** | One arm — an `if`, an `elseif`, or the `else` — a guarding condition (none for `else`) plus its body. |
| **Branch body** | The blocks a branch plays when taken; may hold a nested control block. |
| **Control block** | The whole construct: an ordered list of branches, held in one connected blockquote. |
| **Connected blockquote** | A single `>` container holding every branch, continued across arm boundaries by a bare `>` line; a nested branch is a `> >` container. |

## Prior art

Surveyed from primary sources, block conditionals divide into three **terminator
families**.

| Language | Open / separator / else | Terminator | Family |
| --- | --- | --- | --- |
| Yarn Spinner | `<<if>>` / `<<elseif>>` / `<<else>>` | `<<endif>>` | explicit terminator |
| SugarCube (Twine) | `<<if>>` / `<<elseif>>` / `<<else>>` | `<</if>>` | explicit terminator |
| Ink | `{ cond:` / `- cond:` / `- else:` | `}` | bracket |
| Harlowe (Twine) | `(if:)[` / `(else-if:)[` / `(else:)[` | `]` per hook | bracket / hook |
| Ren'Py | `if:` / `elif:` / `else:` | dedent | indentation |
| Markdown `:::` directives | `:::name` (no separator) | `:::` | container fence |

Two lessons shaped the choice: a block conditional **needs a defined boundary**
(every mature language marks the end explicitly or by dedent), and **Markdown
containers do not separate branches** — Markdig's `CustomContainerParser` has no
branch-separator concept, so a flat `if`/`elseif`/`else` chain in one `:::` block
would need a custom parser (confirmed against the Markdig source). A **blockquote**,
by contrast, is a first-class Markdig container that already nests, so the structure
comes for free.

## Chosen shape

The evaluation behind each choice is condensed here; the alternatives weighed and
rejected are listed under [Alternatives not chosen](#alternatives-not-chosen).

### Marker spelling — two spans

A marker is written as **two code spans** — a keyword span and the verbatim condition
span — `` `if` `` `` `Rich?` `` / `` `elseif` `` `` `Poor?` `` / `` `else` ``.

**Unquoted keys make this decisive.** An unquoted key is *everything before the `?`
sigil, spaces included* (see [Unquoted Keys](./Unquoted%20Keys.md)), so a **one-span**
marker `` `if Rich?` `` would parse as a single condition on the key `if Rich`, not an
`if` guarding `Rich`. The two-span form sidesteps that: `` `if` `` is a bare,
sigil-less, quote-less span — a closed marker vocabulary that collides with nothing (a
condition needs a sigil, a value read needs quotes, a command needs parens) — and
`` `Rich?` `` is the *verbatim* shipped condition, reusing its reader, highlighting,
and completion. It composes even with spaces: `` `if` `` `` `Is Alice rich?` ``.

### Grouping — one connected blockquote

Every branch lives in **one connected blockquote**: `` `if` ``, each `` `elseif` ``,
and the `` `else` `` share a single `>` container, continued across arm boundaries by a
bare `>` line; a nested conditional is a nested `> >` blockquote.

**Chosen for readability and visuals.** A rendered comparison was decisive — three
*separate* blockquotes read as independent asides, while **one connected** blockquote
reads as a single mutually-exclusive construct: one left bar spans all arms, a nested
`if` is *visibly* nested, and a bare jump returning to the outer arm sits at the outer
bar, so a reader sees which branch owns it. The container also supplies the boundary
for free, so **no terminator keyword** (and no `#END` clash) is needed.

**The authoring cost is real but offset by editor support.** Connectedness couples the
arms through unbroken `>`, so each utterance's blank-line separator must be a bare `>`
line, and reflowing means maintaining `>` (and `> >` when nested). DialogueDown already
projects editor semantics from the compiler (see
[Compiler-Projected Editor Semantics](./Compiler-Projected%20Editor%20Semantics.md)
and [Source Editor Autocompletion](./Source%20Editor%20Autocompletion.md)); the same
seam maintains the `>` prefixes as the writer types, completes the markers, and
highlights them — so the friction is an editor affordance, not a manual chore.

### Rendering in a stock preview

The marker spans render as clean monospace **chips** in any previewer, and the
connected blockquote renders as one indented, visibly grouped block. A stock preview
has no directive/admonition plugin and sanitizes HTML, which rules out `:::`
directives (shown literally), `<details>` (collapsed, hidden), and `<aside>`
(sanitizer-stripped); the blockquote needs none of them. The construct's richer view —
an indented branch tree — is also supplied by DialogueDown's
[visualization report](./Semantic%20Model%20Visualization%20Tab.md).

## Writer-facing behavior

A gate guard reacts to the visitor's standing, and — only for the wealthy — to whether
they are armed:

```markdown
> `if` `Rich?`
>
> Guard: Ah — my lord. The gate is yours.
>
> > `if` `Armed?`
> >
> > Guard: But leave your blade at the post.
> >
> > `("disarm the visitor")`
>
> => [The courtyard](#the-courtyard)
>
> `elseif` `Poor?`
>
> Guard: Back to the gutter with you.
>
> `else`
>
> Guard: I don't know your face. What business have you?
```

The `` `if` `` opens the block; each `` `elseif` `` and the final `` `else` `` continue
the **same** blockquote (note the bare `>` separators). A branch body may hold several
utterances, a bare jump, a silent command, or a **nested** conditional (the `> >`
block above). Zero or one branch is taken at play time: the first true guard, the
optional `else`, or none when no guard matches.

A blockquote that is **not** led by a marker is a transparent wrapper: its inner blocks
read as ordinary content, in place.

## Grammar

The construct is expressed over Markdown block structure: a blockquote whose blocks are
marker paragraphs and branch bodies.

```ebnf
(* One connected blockquote: an if-arm, then elseif-arms, then an optional else-arm. *)
ControlBlock = BlockQuote , IfArm , { ElseIfArm } , [ ElseArm ] ;

IfArm        = IfMarker     , BranchBody ;
ElseIfArm    = ElseIfMarker , BranchBody ;
ElseArm      = ElseMarker   , BranchBody ;

IfMarker     = "`" , "if" , "`"     , Condition ;   (* two spans: keyword + condition *)
ElseIfMarker = "`" , "elseif" , "`" , Condition ;
ElseMarker   = "`" , "else" , "`" ;                 (* no condition *)

Condition    = "`" , Key , "?" , "`" ;              (* the shipped condition; Unquoted Keys *)
BranchBody   = { Block } ;                          (* any blocks, incl. a nested ControlBlock *)
```

`BlockQuote` is the Markdown blockquote container; each marker is its own paragraph, a
`BranchBody` is the blocks between one marker and the next (or the block's end), and a
nested `ControlBlock` is a nested `> >` blockquote inside a `BranchBody`. `Condition`,
`Key`, and the effect fragments are unchanged from their notes.

## Architecture

Recognition happens **in the transpiler**, not in a desugar rule. A control block is a
*Markdown-level* shape — a marker-headed blockquote with nested blocks — so it must be
built where that structure is still in hand. (Contrast the [Control Line](./Control%20Line.md),
recognized in desugar because it needs jumps assembled first.) Two things change; the
rest of the pipeline recurses into branch bodies unchanged.

```mermaid
flowchart LR
    MD["Markdig QuoteBlock<br/>(nested > > already nested)"] --> CV["Converter →<br/>Markdown AST QuoteBlock"]
    CV --> BB{"BlockBuilder:<br/>marker-headed quote?"}
    BB -->|"yes"| CBB["ControlBlockBuilder<br/>validate + split markers"]
    CBB --> CB["ControlBlock<br/>bodies recurse through BlockBuilder"]
    BB -->|"no"| RAW["transparent wrapper<br/>(inner blocks in place)"]
```

- **Markdown AST** gains a structural `QuoteBlock` block: the converter maps a Markdig
  `QuoteBlock` to it (holding its converted child blocks) instead of flattening it, so a
  nested `> >` is a nested `QuoteBlock`.
- **The transpiler's `BlockBuilder`** dispatches a `QuoteBlock`: a non-marker quote is a
  transparent wrapper whose inner blocks are transpiled in place; a marker-headed quote
  is delegated to `ControlBlockBuilder`.
- **`ControlBlockBuilder`** validates and splits markers, builds each `Branch`, and calls
  back into its owning `BlockBuilder` for branch bodies. The shared recursion makes a nested
  marker-headed quote a nested `ControlBlock`.

The AST mirrors `Choices`/`Choice`:

```csharp
internal sealed record ControlBlock(IReadOnlyList<Branch> Branches, SourceSpan Span)
    : ScriptBlock(Span);

internal sealed record Branch(
    Condition? Condition, IReadOnlyList<ScriptBlock> Body, SourceSpan Span)
    : ScriptNode(Span), IConditional;   // if/elseif carry a Condition; else is null
```

Because a `Branch.Body` is a list of `ScriptBlock`, every downstream pass reaches inside
it by recursing — the `DialogueAstRewriter`, the `ScriptNodeExtensions` traversal, the
report projection, and the validation rules each gain a `ControlBlock` arm. Each switch
throws on an unknown kind, so the compiler will not build until all handle it — the
completeness the [Control Line](./Control%20Line.md) note relied on.

## Interfaces and responsibilities

| Component                       | Responsibility                                                                            | Change                                           |
| ------------------------------- | ----------------------------------------------------------------------------------------- | ------------------------------------------------ |
| `QuoteBlock` (Markdown AST)     | Hold a blockquote's child blocks structurally.                                            | New; converter stops flattening quotes.          |
| `MarkdigToMarkdownAstConverter` | Map a Markdig `QuoteBlock` to the new node.                                               | Add the case.                                    |
| `MarkerRecognition`             | Read a paragraph as an `` `if` ``/`` `elseif` `` + condition, or `` `else` ``.            | New; reuses the condition reader.                |
| `BlockBuilder`                  | Dispatch a quote to control construction or transparently transpile its inner blocks.     | Add the `QuoteBlock` case.                       |
| `ControlBlockBuilder`           | Validate and split markers, build branches, and recurse through `BlockBuilder`.           | New builder.                                     |
| `ControlBlock` / `Branch` (AST) | Model the construct and its arms; carry spans and each arm's condition.                   | New records.                                     |
| `DialogueAstRewriter`           | Rewrite a `ControlBlock` and each branch body.                                            | New hook (like `RewriteChoice`).                 |
| `ScriptNodeExtensions`          | Enumerate a `ControlBlock`'s branches and each branch's children.                         | New arms.                                        |
| `DialogueAstProjection`         | Project a `ControlBlock` to a report node.                                                | New arm.                                         |
| Validation rules                | Treat a branch condition as a bound guard; reject scene headings inside branch bodies.    | New/extended rules.                              |

## Key design decisions

### D1 — Recognize in the transpiler, from the blockquote structure

A control block is a Markdown structure (a marker-headed blockquote), so
`ControlBlockBuilder` builds it from the `QuoteBlock` passed by `BlockBuilder`, while that
structure is present, rather than reconstructing it later. This reuses the existing
"a container block becomes a grouped Dialogue block" path (a list → `Choices`) and keeps
the nested-quote tree Markdig already built.

### D2 — The marker is the two-span form

`` `if` `` `` `cond?` `` — a bare keyword span plus the verbatim condition. Unquoted keys
make a one-span `` `if Rich?` `` ambiguous (it reads as a condition on the key
`if Rich`), so the keyword is kept a standalone span; the condition span is reused
untouched. See [Chosen shape](#marker-spelling--two-spans).

### D3 — One connected blockquote, enforced; a severed chain is an error

Every arm shares one blockquote. Because the arms are coupled by unbroken `>`, an
accidental plain (non-`>`) blank line **severs** the chain into separate blockquotes —
a break that is *source-invisible*. A severed `` `elseif` ``/`` `else` `` — one that
opens its own blockquote instead of continuing the `` `if` ``'s — is a **strict error**,
never silently re-paired, which turns the invisible break into a clear diagnostic.

### D4 — `ControlBlock` / `Branch` mirror `Choices` / `Choice`

A `ControlBlock` holds ordered `Branch`es; a `Branch` holds a `Body` of blocks and an
optional `Condition`, reusing `IConditional`. Nesting is a nested `ControlBlock` inside a
branch body — the same way a nested list nests inside a choice. No new traversal shape is
introduced; existing recursion reaches inside.

### D5 — Editor support offsets the `>` authoring tax

CodeMirror's Markdown support continues the connected blockquote's `>` prefix when the
writer presses Enter. The compiler-projected editor seam (see
[Compiler-Projected Editor Semantics](./Compiler-Projected%20Editor%20Semantics.md))
highlights recognized `` `if` `` / `` `elseif` `` / `` `else` `` markers without a
second browser grammar. The raw preview stays readable without either aid.

### D6 — A scene is a top-level unit; no heading inside a branch

`SceneBuilder` opens scenes only from **top-level** headings, so a heading nested in a
branch would build no scene and register no anchor — a silent no-op. Rather than support
a *conditional* scene (a conditional anchor would break deterministic jump resolution), a
scene heading inside a branch is a **diagnostic**. To choose a scene by condition, use a
conditional jump to a top-level scene; to make content conditional, keep it inline in the
branch. The same rule generalizes to a heading inside a choice body.

### D7 — Separate every marker and utterance with a quoted blank line

Inside the blockquote each marker and each utterance is its own paragraph, so — as
everywhere in the language — a blank line separates them; here that blank line is
**quoted** (a bare `>` at the current depth). This is less a new rule than the
blockquote form of the existing one, with one sharp corollary: a quoted blank line
is what **closes a nested branch** before the outer branch resumes or an
`elseif`/`else` begins.

The corollary matters because of CommonMark **lazy continuation**: with no blank
line, a later line keeps continuing the open paragraph at the depth it started, so a
missing quoted blank line silently **collapses a dedent** — outer content, or an
`elseif`/`else` marker, is pulled *into* the nested branch — or **fuses a marker with
its body**. The damaging form, a marker swallowed into a nested branch, is detectable
and reported ([Diagnostics](#diagnostics)); a content-only collapse cannot be
recovered once Markdig has merged it, so it is prevented by editor support
([D5](#d5--editor-support-offsets-the--authoring-tax)) maintaining the quoted-blank
separators and documented with a before/after example in the guide.

## Markdown interaction

A **marker-headed** blockquote becomes a `ControlBlock`; every other blockquote is a
**transparent wrapper** whose inner blocks are read in place as ordinary content, so a
plain `> aside` reads as narration rather than literal `> aside` text. Arms are joined by
a bare `>` line and nested by `> >` — both standard CommonMark, so no Markdig extension is
needed. The marker spans are ordinary inline code, so a stock preview renders them as
chips.

## Diagnostics

Five grammar errors are reported during transpilation, before marker-only data is
discarded from the semantic AST. One placement error belongs to validation; two existing
checks recurse into branch bodies.

- **Severed chain** (`DLG1108`) — a blockquote led by `` `elseif` `` or `` `else` ``
  with no connected `` `if` ``. Reported, never re-paired ([D3](#d3--one-connected-blockquote-enforced-a-severed-chain-is-an-error)).
- **Malformed marker order** (`DLG1109`) — an `` `elseif` `` after the `` `else` ``, a
  second `` `else` ``, or a marker the block builder cannot place.
- **Marker not standing alone** (`DLG1110`) — an `` `if` ``/`` `elseif` ``/`` `else` ``
  marker fused into a paragraph (mid-line, or with trailing speech) instead of alone on
  its line; the fix is a quoted blank line. Catches the nested lazy-continuation merge
  ([D7](#d7--separate-every-marker-and-utterance-with-a-quoted-blank-line)).
- **Missing branch condition** (`DLG1111`) — an `` `if` `` or `` `elseif` `` marker
  lacks its required condition span.
- **Unexpected `else` condition** (`DLG1112`) — an `` `else` `` carries a condition;
  recovery keeps it as the unconditional fallback.
- **Orphan condition** (`DLG1106`, reused) — a branch's condition is a *bound* guard, not
  an orphan, exactly as for a conditional line or control line.
- **Unreachable after a jump** (reused) — the [Progression Order](./Progression%20Order.md)
  check still applies to a jump inside a branch.
- **Scene heading inside a branch** (`DLG2015`, validation) — a scene is a top-level unit;
  a heading nested in a branch is reported ([D6](#d6--a-scene-is-a-top-level-unit-no-heading-inside-a-branch)).

## Error and boundary cases

| Input | Reads as | Why |
| --- | --- | --- |
| a connected quote led by `` `if` `` | a `ControlBlock` | first child is an `if` marker |
| a nested `> >` quote led by `` `if` `` | a nested `ControlBlock` | Markdig nests it; `Build` recurses |
| `` `elseif` ``/`` `else` `` opening its own blockquote | severed-chain error | not connected to an `if` |
| a second `` `else` ``, or `` `elseif` `` after `` `else` `` | marker-order error | branches must be `if` , `elseif`* , `else`? |
| `` `if` `` / `` `elseif` `` without a condition | missing-condition error (`DLG1111`) | guarded branches require the second code span |
| `` `else` `` followed by a condition | unexpected-condition error (`DLG1112`) | the fallback is unconditional |
| a scene heading inside a branch | scene-placement error (`DLG2015`) | a scene is a top-level unit |
| a marker fused into a paragraph (no quoted blank line) | marker-standalone error (`DLG1110`) | a marker must be alone on its line |
| a blockquote **not** led by a marker | transparent wrapper (inner blocks in place) | the construct claims only marker-headed quotes |
| `` `if Rich?` `` (one span) | a condition on the key `if Rich` | not a marker — see [D2](#d2--the-marker-is-the-two-span-form) |

## Testability

- **Marker recognition** — `` `if` ``/`` `elseif` `` + condition, and bare `` `else` ``;
  a one-span `` `if Rich?` `` stays a plain condition.
- **Transpile** — a connected blockquote builds a `ControlBlock` with ordered branches;
  a nested `> >` builds a nested `ControlBlock`; a non-marker quote is a transparent wrapper.
- **Diagnostics** — severed chains, malformed order, fused markers, missing conditions,
  and an `else` condition each report; a branch condition is not an orphan.
- **Spans** — the block, each branch, and each condition preserve their source spans.
- **Completeness** — traversal, rewriting, validation, compiler integration, and
  projection each cover `ControlBlock`.

## Alternatives not chosen

- **Terminator marker** (`` `endif` ``) — clean to author and easy to reflow, but renders
  flat (nesting read by matching `if`/`endif`) and needs a terminator keyword that dodges
  the `#END` clash; the connected blockquote grouping was preferred for its visible
  nesting and free boundary.
- **Separate (disconnected) blockquotes per arm** — reads as independent asides and needs
  sibling-chain re-linking; the single connected blockquote reads as one construct
  ([D3](#d3--one-connected-blockquote-enforced-a-severed-chain-is-an-error)).
- **One-span marker** `` `if Rich?` `` — collides with an unquoted key
  ([D2](#d2--the-marker-is-the-two-span-form)).
- **Command-style marker** `` `If(Rich?)` `` — collides with the command form and inverts
  the "a command acts, a condition reads" distinction.

## Crosscheck

| Outcome | Result |
| --- | --- |
| **Achieved** | Connected and nested blockquotes build semantic `ControlBlock` / `Branch` nodes; grammar and placement diagnostics recover without polluting the AST; traversal, desugaring, validation, visualization, and editor highlighting cover the construct. |
| **Changed** | Control construction moved from `BlockBuilder` into a dedicated `ControlBlockBuilder`; malformed marker shapes use five focused transpile diagnostics; marker highlighting combines the Markdown AST's keyword spans with the semantic Dialogue AST rather than retaining marker kinds on `Branch`. Empty and effect-only branch bodies remain valid. |
| **Not implemented** | Runtime branch selection remains deferred to [#45](https://github.com/pengzhengyi/dialoguedown/issues/45). |

## Open questions and deferred work

- **Runtime evaluation** — selecting and playing a branch belongs to the graph/runtime
  ([#45](https://github.com/pengzhengyi/dialoguedown/issues/45)).

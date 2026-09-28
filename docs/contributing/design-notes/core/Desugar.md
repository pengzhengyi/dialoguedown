# Desugar

> [!NOTE]
> Status: **implemented**. The third
> [pipeline stage](../README.md#core-the-compiler-pipeline): an ordered list of
> local rewrite rules over the Dialogue AST — assemble jumps, recognize control
> lines, fill the default speaker — returning a `DesugaredScriptDocument`.

## Table of contents

- [Goal and scope](#goal-and-scope)
- [Ubiquitous language](#ubiquitous-language)
- [The rules](#the-rules)
- [The rewriter](#the-rewriter)
- [Key design decisions](#key-design-decisions)
- [Error and boundary cases](#error-and-boundary-cases)
- [Testability](#testability)

## Goal and scope

The [transpiler](./Markdown%20to%20Dialogue%20AST%20Transpiler.md) is a local
tokenizer and leaves three things in pieces. Desugar composes them, still with
local information only:

| Before | After |
| --- | --- |
| `Line(Alice, [Text "Go ", JumpIndicator, Text " ", Link([Text "on"], "#next")])` | `Line(Alice, [Text "Go ", Jump([Text "on"], "#next")])` |
| `Line(null, [JumpIndicator, Link(…)])` | `ControlLine([Jump(…)])` |
| `Line(null, [CustomCommand GiveGold("5")])` | `ControlLine([CustomCommand …])` |
| `Line(null, [Text "The wind blows."])` | `Line(DefaultSpeaker, [Text "The wind blows."])` |

Anything that needs the whole document — which speaker is the default, merging a
partial declaration's tags, nesting scenes, resolving targets — is the
[semantic analyzer](./Semantic%20Analyzer.md).

## Ubiquitous language

| Term | Meaning |
| --- | --- |
| **Rule** | One normalization over the whole tree, an `IDesugarRule`. |
| **Rewriter** | `DialogueAstRewriter`: an immutable, clone-by-default tree transform. |
| **Jump** | A `Link` folded with its preceding `=>` (and any condition before it): label, unresolved target, optional condition. |
| **Dangling arrow** | A `=>` with no link after it; degraded to `Text("=>")`. |
| **Control line** | An effect-only line with no speaker (see [Control Line](../language/Control%20Line.md)). |
| **DefaultSpeaker** | The sentinel for "whatever the default speaker is", resolved by analysis. |
| **DesugaredScriptDocument** | The desugared tree, typed so analysis cannot receive a raw one. |

## The rules

`ScriptDesugarer` builds a `Desugarer` per compilation through
`DesugarerFactory.CreateDefault(sink)` and runs the rules in order, each feeding the
next:

| # | Rule | Does | Needs |
| --- | --- | --- | --- |
| 1 | `JumpAssemblyRule` (via `JumpAssembler`) | In every fragment list, fold `[Condition] JumpIndicator (same-line whitespace) Link` into a `Jump`; degrade a dangling arrow and report `DLG1113`. | — |
| 2 | `ControlLineRecognitionRule` | A speaker-less line whose speech is only jumps, commands, breaks, and blank text becomes a `ControlLine`. | assembled jumps |
| 3 | `DefaultSpeakerRule` (via `DefaultSpeakerFiller`) | A remaining speaker-less `Line` gets `DefaultSpeaker` at a zero-width span at the line start. | control lines already removed |

After desugar, no `JumpIndicator` survives and no `Line` has a null speaker.

## The rewriter

`DialogueAstRewriter` rebuilds each node from its rewritten children, an identity
transform by default. It has a `protected virtual` hook for every node kind that
holds children — blocks, lines, control lines, speakers, scene headings, choices,
random choices and options, control blocks and branches, every fragment list, each
fragment, and tags — so a rule overrides only the hook it changes and never repeats
the traversal. A null speaker is handled at the `Line`, so `RewriteSpeaker` only sees
a present one. `DesugarRule` is the base: a rewriter whose `Apply` rewrites the whole
document.

## Key design decisions

### D1 — Local rewrites only

Every rule decides from adjacent fragments or one line, so desugar is a total
`ScriptDocument → ScriptDocument` function and never needs the document-wide
tables analysis builds.

### D2 — An ordered rule list

Each normalization is one independently tested rule; a new one is one entry in the
factory's list. The order is a dependency: control-line recognition needs assembled
jumps, and must run before the default-speaker fill so an effect is never
attributed to a speaker.

### D3 — A jump is single-line, and a dangling arrow degrades

Only blank, same-line text may sit between `=>` and its link; a `LineBreak` ends
the scan. A dangling arrow becomes `Text("=>")` at the arrow's own span, and the
warning is reported here, the one place that still knows it was an arrow (see
[Dangling Arrow Diagnostic](../diagnostics/Dangling%20Arrow%20Diagnostic.md)). The
rule list is built per compilation because `JumpAssemblyRule` holds that
compilation's sink while `ScriptDesugarer` is a singleton.

### D4 — The default speaker is a zero-width sentinel

`DefaultSpeaker` has no source text, so it carries `SourceSpan.EmptyAt(line start)`:
a tool renders a caret there rather than underlining the whole line.

### D5 — Same node types, a document-level stage marker

Desugar adds no parallel "normalized AST"; `Jump`, `ControlLine`, and
`DefaultSpeaker` are ordinary Dialogue AST nodes that appear only after it.
`DesugaredScriptDocument` wraps the result so the pipeline type-checks in order: the
marker attests that desugar ran, and tests uphold the invariants.

## Error and boundary cases

| Case | Behavior |
| --- | --- |
| `the => arrow` | `=>` degrades to `Text` at its own span; `DLG1113` warning. |
| `=> x [a](#b)` | Dangling: only whitespace may separate `=>` from the link; the link stays bare. |
| `=>` then a line break then a link | Dangling; the link stays bare. |
| `=>[a](#b)` | Assembled; no whitespace to fold. |
| Several jumps in one speech | Each pair assembled; `DLG1003` later flags content after the first. |
| `=>` inside `StyledText`, a choice, or a branch | Assembled or degraded in place. |
| A line with a speaker | Unchanged. |
| A speaker-less line with spoken content | Gets `DefaultSpeaker`. |
| Declarations, references, partial declarations | Unchanged; resolved by analysis. |
| Empty document | Unchanged. |

## Testability

- **Rules** are tested alone on hand-built fragments and lines, asserting with
  `DialogueAstAssert` helpers such as `AssertJump`.
- **Rewriter:** an identity subclass leaves every node kind equal, and small
  subclasses that change one kind prove the hooks reach everywhere.
- **Desugarer:** end to end on transpiled scripts, asserting the invariants.

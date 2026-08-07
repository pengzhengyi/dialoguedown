# Compilation outcome

> [!IMPORTANT]
> **Status: in progress.** Splits `CompilationResult` into a success/failure
> pair so a compile that produced a runnable graph is a different type from one
> that did not. Prompted by the [Dialogue Graph](./Dialogue%20Graph.md) stage,
> whose artifact exposed the strain in the single-class model.

## Table of contents

- [Goal and scope](#goal-and-scope)
- [Why the single class strains](#why-the-single-class-strains)
- [Ubiquitous language](#ubiquitous-language)
- [Functionality checklist](#functionality-checklist)
- [Chosen shape](#chosen-shape)
  - [Success carries every artifact](#success-carries-every-artifact)
  - [Failure carries how far it got](#failure-carries-how-far-it-got)
  - [Reaching a stage is not succeeding](#reaching-a-stage-is-not-succeeding)
- [Decisions](#decisions)
- [Error and boundary cases](#error-and-boundary-cases)
- [Migration](#migration)
- [Testability](#testability)

## Goal and scope

A compile runs the stages — parse, transpile, desugar, validate, analyze, build
the graph — and either produces the **dialogue graph** a runtime walks, or it
does not. Today both endings share one `CompilationResult` whose later artifacts
are nullable and whose accessors throw, so the type cannot say which ending it
is; a caller asks flags instead.

This note replaces that with a **closed pair**: `CompilationSuccess` carries
every artifact, non-null; `CompilationFailure` carries the artifacts the compile
reached before stopping. The shared surface stays on the abstract
`CompilationResult`, so no public signature changes.

Out of scope: the stages themselves, the [compilation
mode](./Compilation%20Mode%20Configuration.md) policy that decides *when* to
halt, and the visualization's rendering of unavailable stages.

## Why the single class strains

Adding the graph created a third state the class was not designed for.

| Compile | `IsComplete` | Artifacts | What `NotProduced` claims |
| --- | --- | --- | --- |
| Halted after an erroring transpile | false | markdown, script | "halted at an earlier stage" — **true** |
| Ran every stage, has errors | **true** | all but the graph | "halted at an earlier stage" — **false** |
| Ran every stage, clean | true | all | — |

The middle row is the new one, and it breaks the model three ways:

- `IsComplete` reads as "the compile finished" but means "semantics exist".
- The graph needs a *second* flag, `HasGraph`, to answer the same kind of question.
- The graph cannot reuse the shared `NotProduced` message, because that message
  is wrong for the case that produces it.

Three flags and two exception messages for one question — *what did this compile
produce?* — is the signal that the question belongs in the type.

## Ubiquitous language

| Term | Meaning |
| --- | --- |
| **Compilation result** | The abstract outcome of one compile: its source, its diagnostics, and which ending it is. |
| **Compilation success** | A compile that produced a dialogue graph. Every stage artifact is present. |
| **Compilation failure** | A compile that produced no graph, carrying the artifacts it reached. |
| **Reached** | A stage ran and produced its artifact — independent of whether the compile succeeded. |

> [!NOTE]
> A **warning** does not make a compile a failure. Only an error does, since only
> an error means the recovered model no longer describes what the writer wrote.

## Functionality checklist

- [ ] Make `CompilationResult` abstract, keeping `Source`, `Diagnostics`, `HasErrors`, and `LocatedDiagnostics` on it.
- [ ] Add `CompilationSuccess` with every stage artifact non-null, including the `Graph`.
- [ ] Add `CompilationFailure` with a named factory per stage a compile can stop at.
- [ ] Retire `IsComplete`, `HasGraph`, and the throwing artifact accessors.
- [ ] Keep the public surface unchanged, so no caller outside the facade is broken.
- [ ] Project the pair in the visualization by matching the outcome, not by reading a flag.

## Chosen shape

```csharp
public abstract record CompilationResult(string Source, IReadOnlyList<Diagnostic> Diagnostics)
{
    public bool HasErrors { get; }                        // any diagnostic is an error
    public IReadOnlyList<LocatedDiagnostic> LocatedDiagnostics { get; }
}

public sealed record CompilationSuccess : CompilationResult
{
    internal MarkdownDocument Markdown { get; }           // every artifact, never null
    internal ScriptDocument Script { get; }
    internal DesugaredScriptDocument Desugared { get; }
    internal SemanticModel Semantics { get; }
    internal DialogueGraph Graph { get; }
}

public sealed record CompilationFailure : CompilationResult
{
    internal MarkdownDocument Markdown { get; }           // always reached
    internal ScriptDocument Script { get; }
    internal DesugaredScriptDocument? Desugared { get; }  // reached only past the transpile halt
    internal SemanticModel? Semantics { get; }

    internal static CompilationFailure AtTranspile(...);  // the stage it stopped at
    internal static CompilationFailure AtAnalysis(...);
}
```

### Success carries every artifact

A success is defined by having produced the graph, so nothing on it is optional
and nothing throws. A caller writes what it means:

```csharp
if (compiler.Compile(source) is CompilationSuccess success)
{
    Run(success.Graph);
}
```

### Failure carries how far it got

A failure is *expected* to be partial, so optional artifacts are honest there.
Named factories keep the reachable combinations the only representable ones —
`AtTranspile` cannot carry a semantic model, and a state where semantics exist
without a desugared tree cannot be constructed at all. A future desugar
checkpoint adds a third factory rather than another nullable column.

### Reaching a stage is not succeeding

The two questions are independent, and conflating them would regress the
visualization, which exists to show what a **broken** script still produced:

| Compile | Outcome | Desugared and Semantics |
| --- | --- | --- |
| Halted after an erroring transpile | failure | not reached |
| Ran every stage, has errors | failure | **reached — and shown** |
| Ran every stage, clean | success | reached |

So a failure exposes its artifacts, and a tool projecting stages reads those
rather than the outcome.

## Decisions

- **A closed pair, not a flag.** The outcomes differ in *what they hold*, not
  just in a label, so they differ in type. This is the same reasoning the graph
  uses for its node kinds: a kind is its type, not a discriminator field.
- **Nullable artifacts are honest on failure and dishonest on success.** The
  single class applied the failure shape to both, which is why success needed
  runtime guards for an invariant it always satisfied.
- **The base keeps only what both endings share.** `Source`, `Diagnostics`,
  `HasErrors`, and `LocatedDiagnostics` are exactly the members that mean the
  same thing either way — and exactly the current public surface, so the split
  costs no caller anything.
- **Failure is named by the stage it stopped at,** not by a `Stage` enum beside
  nullable artifacts. The factory is the place that knows which artifacts exist,
  so it is the place that should enforce it.
- **A warning never fails a compile.** Failure means an error, so a script that
  compiles with advice still produces a runnable graph.

## Error and boundary cases

| Case | Outcome |
| --- | --- |
| Empty document | Success — the graph is the `EndNode` alone. |
| Errors, best-effort mode | Failure from `AtAnalysis`: every earlier artifact reached, no graph. |
| Errors, stage-boundary mode, erroring transpile | Failure from `AtTranspile`: no desugared tree, no semantics. |
| Warnings only | Success — a warning is advice, not a broken script. |
| Fail-fast mode | No result at all; the sink throws before an outcome exists. |

## Migration

The split is internal to the facade. `IScriptCompiler.Compile` keeps returning
`CompilationResult`, now the abstract base, and the only consumer outside the
facade — the visualization — swaps its `IsComplete` ternary for a match on the
outcome.

## Testability

Each ending is constructible on its own, so a test states the outcome it means
rather than assembling a result and asserting a flag. The factories make an
unreachable failure shape a compile error rather than a case a test must cover.

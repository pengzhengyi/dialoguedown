# Dangling arrow diagnostic

> [!NOTE]
> Status: **proposed**
> ([issue #227](https://github.com/pengzhengyi/dialoguedown/issues/227)).
> Warns when a `=>` has no link after it, so the jump the writer intended is
> silently degraded to the literal characters `=>`.

## Table of contents

- [Goal and scope](#goal-and-scope)
- [Functionality checklist](#functionality-checklist)
- [Ubiquitous language](#ubiquitous-language)
- [Writer-facing behavior](#writer-facing-behavior)
- [Why it happens today](#why-it-happens-today)
- [Architecture](#architecture)
- [Interfaces and responsibilities](#interfaces-and-responsibilities)
- [Key design decisions](#key-design-decisions)
- [Error and boundary cases](#error-and-boundary-cases)
- [Integration](#integration)
- [Testability](#testability)
- [Alternatives not chosen](#alternatives-not-chosen)

## Goal and scope

A jump is `=>` followed by a Markdown link. When the link is missing —

```markdown
=> The market
```

— the arrow is **dangling**. Desugar degrades it to the plain characters `=>`, so
the script still compiles and the line simply reads "=> The market". The writer
sees no error, no warning, and no jump. That silent degradation is the worst
failure mode for an author: nothing looks wrong, but the intended flow is gone.

This note adds a **warning** at the moment desugar drops the arrow, so the writer
learns their jump did not become a jump.

**In scope:** a `Syntax` diagnostic with `Warning` severity reported by the
**desugar** stage, threading a diagnostic sink into the jump assembler, the
writer-facing guidance, and the generated error-code entry.

**Out of scope:** changing how a dangling arrow *behaves* (it still degrades to
text — we warn, we do not fail the compile or invent a jump target); the second
half of [#227](https://github.com/pengzhengyi/dialoguedown/issues/227) (a
front-end record of dropped unmodeled Markdown), which is a separate component
and the prerequisite for [#47](https://github.com/pengzhengyi/dialoguedown/issues/47).

## Functionality checklist

- [ ] Add a `Syntax` diagnostic with `Warning` severity for a dangling arrow.
- [ ] Report it from **desugar**, at the point the arrow is degraded to text.
- [ ] Point the diagnostic at the arrow's own span (the `=>` characters).
- [ ] Report a **conditional** dangling arrow too (`` `Ready?` => `` with no link),
      pointing at the arrow rather than the guard.
- [ ] Report **every** dangling arrow in a document, not just the first.
- [ ] Do **not** report when the arrow is part of a well-formed jump.
- [ ] Do **not** report for a literal `=>` the writer escaped or wrote as code.
- [ ] Keep the existing degradation behavior byte-for-byte (still `Text("=>")`).
- [ ] Add the generated error-code reference entry and writer-facing guidance.

## Ubiquitous language

| Term | Meaning |
| --- | --- |
| **Jump indicator** | The `=>` token, modeled as `JumpIndicator` before assembly. |
| **Dangling arrow** | A `JumpIndicator` with no `Link` after it, so no `Jump` can be folded. |
| **Degrade** | Replacing the dropped indicator with the literal `Text("=>")` it came from. |
| **Jump assembly** | The desugar step that folds `[condition] => link` into one `Jump`. |

## Writer-facing behavior

Given a script:

```markdown
# The Crossroads

=> The market
```

The compiler reports `DLG1113` — title **Dangling jump arrow**, category
`Syntax`, severity `Warning`:

```text
scene.dialogue.md(3,1): warning DLG1113: This `=>` has no link after it, so it is
not a jump and reads as the characters "=>". Add a link target, such as
`=> [The market](#the-market)`.
```

The line still renders as `=> The market`; the warning explains why.

## Why it happens today

`JumpAssembler` expresses jump assembly as a small parser grammar. A dangling
arrow is matched by an explicit fallback that rewrites the indicator to text:

```csharp
// A => with no link after it is not a jump, so it degrades to the characters "=>".
private static readonly Parser<InlineFragment, InlineFragment> _danglingArrow =
    OfType<JumpIndicator>().Select(indicator => (InlineFragment)new Text("=>", indicator.Span));
```

The assembler knows exactly when it drops an arrow — it just has nowhere to say
so, because it is a static, sink-less helper. `ScriptDesugarer` already receives a
`DiagnosticsContext` and carries a standing `TODO` naming this very case:

```csharp
// TODO(diagnostics): the context is validated but not yet read — desugaring works off
// the tree and the spans it already carries. Report warnings into context.Diagnostics
// when the producers land (e.g. a dangling arrow or multiple jumps).
```

This component makes desugar the **first producer** to use that context.

## Architecture

The sink threads from the stage entry point down to the assembler that owns the
knowledge. Desugar becomes a diagnostic **producer**, joining the transpiler.

```mermaid
flowchart LR
    C["ScriptCompiler:<br/>owns the DiagnosticsContext"] --> SD["ScriptDesugarer:<br/>builds a desugarer per compile"]
    SD --> F["DesugarerFactory:<br/>CreateDefault(sink)"]
    F --> R["JumpAssemblyRule:<br/>holds the sink"]
    R --> A["JumpAssembler:<br/>finds an arrow with no link"]
    A --> S["IDiagnosticSink:<br/>reports DLG1113"]
    A --> T["Text fragment:<br/>the two characters, as prose"]
```

Because `ScriptDesugarer` is a DI singleton, the per-compilation sink cannot live
on a long-lived rule. The desugarer is therefore **built per compilation** with
the sink injected into the reporting rule — see
[DD2](#dd2--construct-the-reporting-rule-per-compilation-not-per-process).

## Interfaces and responsibilities

| Type | Responsibility | Collaborators |
| --- | --- | --- |
| `DiagnosticCatalog` | Owns the new `DLG1113` descriptor. | — |
| `ScriptDesugarer` | Builds a desugarer per compile, wired to `context.Diagnostics`. | `DesugarerFactory`, `DiagnosticsContext` |
| `DesugarerFactory` | Creates the rule pipeline, with and without a sink. | rules |
| `JumpAssemblyRule` | Holds the sink for one compile and hands it to the assembler. | `JumpAssembler` |
| `JumpAssembler` | Reports a dangling arrow as it degrades it. | `IDiagnosticSink` |

## Key design decisions

### DD1 — Report at the drop site, not from a later rule

The diagnostic is produced by `JumpAssembler` at the moment it degrades the
arrow, rather than by a `StructuralValidator` rule afterwards.

Validation runs **after** desugar, and by then a dangling arrow is
indistinguishable from a writer who literally typed `=>` in prose — both are
`Text("=>")`. A later rule could only guess. Reporting where the knowledge exists
keeps the diagnostic exact and needs no new model state to carry the signal.

The cost is that the assembler becomes diagnostic-aware, which
[DD2](#dd2--construct-the-reporting-rule-per-compilation-not-per-process) keeps contained.

### DD2 — Construct the reporting rule per compilation, not per process

`ScriptDesugarer` is registered as a **DI singleton** and holds its rules in a
field built once by `DesugarerFactory.CreateDefault()`, so a rule instance
outlives any single compilation. Storing a per-compilation sink on a long-lived
rule would leak one compile's sink into the next.

Threading the sink as an *argument* is the transpiler's precedent
(`BlockBuilder.Build(blocks, diagnostics)`), but desugar's rules extend
`DialogueAstRewriter`, whose ~14 `protected virtual` hooks take no sink. Adding
one to each would ripple through every rewriter in the codebase for the benefit
of a single rule.

Instead, **build the desugarer per compilation** and give the reporting rule its
sink at construction:

```csharp
public DesugaredScriptDocument Desugar(ScriptDocument document, DiagnosticsContext context)
{
    var desugarer = DesugarerFactory.CreateDefault(context.Diagnostics);
    return new DesugaredScriptDocument(desugarer.Desugar(document));
}
```

`JumpAssemblyRule` then holds the sink for exactly one compile, and the rewriter
hierarchy is untouched. The rules are cheap, stateless value-like objects, so
constructing them per compile costs nothing measurable and removes the shared
mutable state a singleton rule would otherwise carry. The factory keeps a
sink-less overload for callers that do not report.

### DD3 — Warning, not error

A dangling arrow still compiles and still renders — the script runs, only the
jump is missing. A `Warning` tells the writer without failing a build that works
today, matching the severity of the other "silently degraded" diagnostics
(`DLG1003` unreachable content, `DLG1107` styled speaker prefix).

### DD4 — Point at the arrow, not the guard

A conditional dangling arrow (`` `Ready?` => ``) reports at the **arrow's** span.
The guard is valid; the arrow is what failed to become a jump, so that is where
the writer must act.

## Error and boundary cases

| Case | Behavior |
| --- | --- |
| `=> [Label](#target)` | Well-formed jump. No diagnostic. |
| `=> The market` | Dangling. One warning at the `=>`. |
| `` `Ready?` => `` (no link) | Dangling. One warning at the `=>`, not the condition. |
| Two dangling arrows in one document | Two warnings, one per arrow. |
| `=>` inside a code span or escaped | Never becomes a `JumpIndicator`, so no diagnostic. |
| Arrow inside a choice option or control branch | Reported — the rewriter reaches nested sequences. |
| `=>` followed by a link on the **next line** | Already dangling today (a jump is single-line); now warned. |

## Integration

- **Compilation modes.** The diagnostic is a warning, so it never halts a
  stage-boundary compile; `ScriptCompiler` needs no new checkpoint.
- **CLI.** Rendered by the existing errata renderer with no change.
- **Report and LSP.** Flow through the shipped diagnostics overlay and projection.
- **Docs.** The generated error-code reference gains a `DLG1113` entry; the
  language guide's jump section gains a short note.

## Testability

- **Unit — catalog:** `DLG1113` exists, is `Syntax`, and is `Warning`.
- **Unit — assembler:** a dangling arrow reports once and still degrades to
  `Text("=>")`; a well-formed jump reports nothing.
- **Unit — conditional:** the reported span is the arrow's, not the guard's.
- **Integration — pipeline:** compiling a document with a dangling arrow surfaces
  exactly one `DLG1113` with the right span; a clean script surfaces none.
- **Docs test:** the existing diagnostic-catalog Markdown test covers the new
  entry automatically.

## Alternatives not chosen

| Alternative | Why not |
| --- | --- |
| A validation rule after desugar | Cannot distinguish a degraded arrow from prose `=>`; would need new model state purely to carry the signal ([DD1](#dd1--report-at-the-drop-site-not-from-a-later-rule)). |
| Keep the `JumpIndicator` in the tree and report later | Breaks the desugared-tree invariant that no `JumpIndicator` survives, and every downstream consumer would need to handle it. |
| Make it an error | Fails scripts that compile and render today ([DD3](#dd3--warning-not-error)). |
| Guess the target from the following text | Inventing a jump the writer did not write is worse than saying nothing. |

# Control Line

> [!NOTE]
> Status: **implemented**. A **control line** is an effect-only block with no
> speaker — a bare jump or a silent command — so neither is attributed to the
> default speaker. It compiles to a `control` node, which the
> [runner](../runtime/Runner.md) plays: it asks the host to perform each effect and
> waits, or walks past a control node with no effects.

## Table of contents

- [Goal and scope](#goal-and-scope)
- [Ubiquitous language](#ubiquitous-language)
- [Writer-facing behavior](#writer-facing-behavior)
- [Grammar](#grammar)
- [Architecture](#architecture)
- [Interfaces and responsibilities](#interfaces-and-responsibilities)
- [Key design decisions](#key-design-decisions)
- [Diagnostics](#diagnostics)
- [Error and boundary cases](#error-and-boundary-cases)
- [Testability](#testability)
- [Alternatives not chosen](#alternatives-not-chosen)

## Goal and scope

A line is *spoken*: it belongs to a speaker who says its speech. Two constructs
are not speech at all — they are **effects**:

- a **bare jump** on its own line (`=> [The cave](#cave)`), which diverts the
  reading;
- a **silent command** on its own line (`` `("open the gate")` ``), which changes
  game state.

If both were a `Line` with no speaker, the desugarer's default-speaker fill would
give them the configured default speaker — so a game whose default is a named
character would have that character "say" a jump. A line's missing speaker would
mean two things: *spoken by the default* (narration) and *not spoken at all* (an
effect).

A **control line** is an effect-only block that has **no speaker field**, so an
effect is never attributed to a speaker. [Block Controls](./Block%20Controls.md)
reuses the same "control, not speech" idea for its markers.

Scope:

- The **control line** node: an effect-only block holding jump and command
  fragments, with an optional condition, and no speaker.
- Compile-time recognition, preservation, spans, and the traversal, validation, and
  report seams a new block kind touches.

Out of scope: the control block (`if`/`elseif`/`else`), owned by
[Block Controls](./Block%20Controls.md).

## Ubiquitous language

The domain term is **control line**, the effect-only counterpart to a spoken
**line**. The **condition**, **jump**, and **command** terms carry over unchanged.

| Term                | Meaning                                                                                                                                                                     |
| ------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Spoken line**     | A `Line` attributed to a speaker — a named one, or the configured default (narration).                                                                                      |
| **Control line**    | An effect-only block with no speaker: a bare jump, or one or more silent commands.                                                                                          |
| **Effect fragment** | A fragment that acts rather than speaks — a `Jump` (control flow) or a command (`DefaultCommand`/`CustomCommand`). A `Query` is **not** an effect: it produces spoken text. |

## Writer-facing behavior

Nothing new is typed. A bare jump and a silent command on their own line are
control lines:

```markdown
Guide: The gate is open. Go on through.

`("open the gate")`

=> [The courtyard](#courtyard)
```

The command and the jump are **effects**, not lines the guide (or a configured
default speaker) speaks. A line that carries prose but no speaker is still
**narration** by the default speaker, unchanged:

```markdown
The gate swings open with a groan.
```

A leading [condition](./Conditions.md) guards a control line. Before a bare jump
the condition binds to the jump itself; before a silent command it binds to the
control line:

```markdown
`GateJammed?` `("force the gate")`
```

## Grammar

There is **no new surface syntax**. A control line is recognized from an existing
speaker-less line whose content is entirely effects:

```ebnf
ControlLine = [ Condition ] , { Whitespace } , Effect , { Effect | Whitespace } ;
Effect      = Jump | Command ;
```

`Condition`, `Jump`, and `Command` are unchanged from their notes; a control line
reuses their recognition rather than re-deriving it.

## Architecture

Recognition is one **rule in the desugar pipeline**. Desugar runs an ordered list
of rules, each rewriting the whole tree (see the [Desugar](../core/Desugar.md) note):
jump assembly, then **control-line recognition**, then the default-speaker fill.
Recognition sits between the other two on purpose — after jump assembly, so a bare
`=>` run is already a `Jump` and "effect-only" is decidable; and before the fill,
so a control line is never given a speaker.

```mermaid
flowchart LR
    JA["JumpAssemblyRule:<br/>assemble jumps"] --> CR{"ControlLineRecognitionRule:<br/>speaker-less and effect-only?"}
    CR -->|"yes"| CL["ControlLine<br/>(no speaker)"]
    CR -->|"no"| DS["DefaultSpeakerRule<br/>→ spoken Line"]
```

A `ControlLine` is a `ScriptBlock`, so it flows through the pipeline beside `Line`,
`Choices`, and `SceneHeading`. Because the block switches are exhaustive and throw
on an unknown kind, adding the type forces every one of them to handle it — a
completeness the compiler enforces.

## Interfaces and responsibilities

| Component | Responsibility |
| --- | --- |
| `ControlLine` (AST block) | Hold the effect fragments, the span, and an optional `Condition`; expose no speaker. |
| `IConditional` (interface) | Expose a `Condition?` across conditional nodes; `IsConditional` is an extension method over it. |
| `ControlLineRecognitionRule` | Recognize a speaker-less, effect-only line as a `ControlLine`, after jump assembly. |
| `DefaultSpeakerFiller` | Fill the default speaker on spoken lines only; never see a control line. |
| `DialogueAstRewriter` | Rewrite a `ControlLine` (its effects and condition) with a block hook. |
| `ScriptNodeExtensions` | Enumerate a `ControlLine`'s children (its effects) for traversal. |
| `DialogueAstProjection` | Project a `ControlLine` to a report node with a control category. |
| `OrphanConditionRule` | Treat a `ControlLine`'s condition as bound, not an orphan. |
| `UnreachableAfterJumpRule` | Apply the after-a-jump reachability check to a jump on a control line. |

## Key design decisions

### D1 — A distinct sibling type, conditional through `IConditional`

The root smell is that `Line.Speaker` is nullable and **overloaded**: `null` means
both *"spoken, default speaker"* and *"not spoken."* A boolean such as
`Line.IsControl` would keep that overload and push a branch onto every consumer. A
distinct `ControlLine` type carries the distinction in the type system: a control
line simply **has no speaker field**, so "an effect has no speaker" is
unrepresentable otherwise — the SOLID, domain-driven choice.

`ControlLine` is a **sibling** of `Line` under `ScriptBlock`, not a derived class
under a new shared line base. The two share almost no *behavior* to hoist — every
block consumer switches and diverges per concrete type — and the one field they do
share, an optional `Condition`, is **not** line-specific: it already recurs on
`Choice`, `RandomOption`, and `Jump`. So the condition is modeled as a small capability
interface, `IConditional` (a `Condition?`), implemented by all of them, with
`IsConditional` as an **extension method** over the interface. A shared abstract
line base, by
contrast, would rename the most common domain word, invent a base with no natural
name, and still miss that cross-cutting condition (see
[alternatives](#alternatives-not-chosen)).

### D2 — The boundary is speaker-less and effect-only

A line becomes a control line only when it names **no speaker** *and* its content is
**entirely effect fragments** (`Jump`, `DefaultCommand`, `CustomCommand`) plus
whitespace. This preserves two spoken cases:

- **Narration** — a speaker-less line with prose stays a `Line` filled with the
  default speaker, as intended.
- **An inline effect in speech** — `Guide: Follow me. => [Cave](#cave)` keeps its
  speaker, so it stays a spoken `Line` that happens to carry an effect.

A `Query` is deliberately **not** an effect: it reads state to produce spoken text,
so a line containing one is speech.

### D3 — Recognize as a rule in the desugar pipeline

Desugar composes its normalizations as an ordered pipeline of rules (see the
[Desugar](../core/Desugar.md) note), so recognition is its own
`ControlLineRecognitionRule` rather than logic woven into the desugarer. It is
ordered after jump assembly — a bare jump is assembled from raw `=>` text and a
link first, so "effect-only" is decidable without duplicating jump-precursor
detection — and before the default-speaker fill, so a recognized control line is
never given a speaker.

### D4 — The default-speaker fill no longer covers effects

A silent command and a bare jump are `ControlLine`s the default-speaker fill never
sees, so an effect is never attributed to the default speaker.

### D5 — A control line reuses the effect fragments and may carry a condition

A `ControlLine` holds an ordered `IReadOnlyList<InlineFragment>` of effects, reusing
the existing `Jump` and command nodes rather than inventing effect types, and keeps
their spans. It carries an optional `Condition`, so a conditional bare jump or a
conditional silent command is a conditional control line; the condition follows the same
rule as every other [condition](./Conditions.md).

### D6 — Every block switch handles the new kind

Adding a `ScriptBlock` kind touches every exhaustive block switch — the AST
rewriter, the traversal helper, the report projection, and the validation rules
that inspect blocks. Each throws on an unknown kind, so the compiler will not build
until all handle a `ControlLine` — the architecture makes the change complete by
construction.

## Markdown interaction

None. A bare jump and a silent command are ordinary Markdown paragraphs; only
their modeling downstream differs.

## Diagnostics

No diagnostic of its own. Two rules cover the kind:

- **Orphan condition** — a condition that guards a control line's effect is a bound
  condition, detected by identity, not an orphan.
- **Unreachable after a jump** — the [Progression Order](./Progression%20Order.md)
  reachability check applies to a jump whether it sits on a spoken line or a control
  line.

## Error and boundary cases

- A lone condition with no following effect or prose guards nothing (`DLG1106`).
- A line mixing prose and an effect keeps its speaker (or the default) and stays a
  spoken line; it is not a control line.
- Several silent commands on one line form one control line holding each command in
  source order.

## Testability

- **Recognition** — a bare jump and a silent command become a `ControlLine`;
  speaker-less prose stays default-narration `Line`; a speaker plus an effect stays
  a spoken `Line`.
- **Desugar** — a `ControlLine` receives no default speaker; a narration line still
  does.
- **Completeness** — traversal, rewriting, and projection each handle a
  `ControlLine`; an architecture test asserts **no `ControlLine` exposes a speaker**.
- **Validation** — a control line's condition is not reported as an orphan, and an
  unreachable effect after a jump is still caught.
- **Spans** — the control line and each effect preserve their source spans.

## Alternatives not chosen

- **A flag on `Line`** (`IsControl`) — rejected in [D1](#d1--a-distinct-sibling-type-conditional-through-iconditional):
  it keeps the overloaded speaker and scatters branches across consumers.
- **A shared abstract line base** (`SpokenLine`/`ControlLine` under a new base) —
  rejected in [D1](#d1--a-distinct-sibling-type-conditional-through-iconditional): the
  two share little behavior to hoist, it renames the most common domain word, its
  base has no natural name, and it still misses the cross-cutting condition that the
  `IConditional` interface captures.
- **A "system" speaker sentinel** — attributing effects to a reserved non-character
  speaker keeps them inside the speaker model, which is exactly the coupling this
  note removes.
- **Recognizing in the transpiler** — rejected in [D3](#d3--recognize-as-a-rule-in-the-desugar-pipeline):
  a jump is not yet assembled there, so it would duplicate jump-precursor detection.

# Conditions

> [!NOTE]
> Status: **implemented**. The compiler recognizes a condition (`` `key?` ``),
> binds it to the jump, line, control line, choice option, or control branch it
> fronts, and carries it into the playbook. The runner asks the world about it
> (see [Asking the World](../runtime/Asking%20the%20World.md)), except on a choice
> option, which waits until the runner plays choices.

## Table of contents

- [Ubiquitous language](#ubiquitous-language)
- [The primitive](#the-primitive)
- [Where a condition attaches](#where-a-condition-attaches)
- [Conditional jump](#conditional-jump)
- [Conditional line](#conditional-line)
- [Conditional choice option](#conditional-choice-option)
- [Resolution](#resolution)
- [Key design decisions](#key-design-decisions)
- [Diagnostics](#diagnostics)
- [Error and boundary cases](#error-and-boundary-cases)
- [Deferred work](#deferred-work)

## Ubiquitous language

| Term | Meaning |
| --- | --- |
| **Condition** | A game-state query read as a boolean: `` `key?` ``. The one word for it in code, diagnostics, the guide, and the changelog. |
| **Guard-first** | The condition is written *before* what it guards, so it reads "if … then …". |
| **Leading condition** | A condition code span at the start of a block. It is read off before the rest is parsed, so the condition becomes a property of the block rather than its content. |
| **Bound** | A condition that is exactly the `Condition` its parent jump, line, control line, option, or branch references. Any other condition guards nothing. |

## The primitive

A condition is the query the writer already knows, with a `?` sigil. It is the
third member of the query-and-sigil family:

| Syntax | Meaning |
| --- | --- |
| `` `"key"` `` | Insert the query's value into speech (always quoted). |
| `` `key%` `` | Weight a random option by the value. |
| `` `key?` `` | Read the value as a boolean condition. |

```ebnf
Condition   = "`" , Key , "?" , "`" ;
Key         = UnquotedKey | QuotedString ;   (* unquoted is the default *)
UnquotedKey = NonSigilText ;                  (* trimmed, non-empty; spaces allowed *)
```

The key is everything before the `?`, so `` `Is Alice happy?` `` reads the key
`Is Alice happy`. Quotes are the escape for a key that ends in `?`:
`` `"Rainy?"?` `` reads the key `Rainy?`. [Unquoted Keys](./Unquoted%20Keys.md)
owns the key grammar; `ConditionReader` recognizes the span through the shared
`QueryKeyReader`.

## Where a condition attaches

One primitive, five attachment points. The construct decides where the condition
is recognized and what a false answer means.

| Attach point | Where the condition is bound | Playbook field | When false | Example |
| --- | --- | --- | --- | --- |
| Jump | Inline fragment, bound to the following `=>` in desugar (`JumpAssembler`) | `condition` on the `divert` edge | The jump does not fire; reading continues with the next block | `` `FoundKey?` => [Open the vault](#the-vault) `` |
| Line | Read off at the block start, before the speaker (`LineBuilder`) | `condition` on the `line` node | The line is skipped whole | `` `Angry?` Guard: You again? Get out. `` |
| Control line | Read off as for a line, then carried by the [control line](./Control%20Line.md) | `condition` on the `control` node | The effect is not performed | `` `GateJammed?` `("force the gate")` `` |
| Choice option | Read off at the list item, before the weight and body (`ChoiceConditionRecognition`) | `condition` on the `option` or `random-option` edge | A player option is shown **unavailable**; a random option is excluded and the rest re-normalized | `` - `HasKey?` Use the key on the lock. `` |
| Control branch | After the `` `if` `` / `` `elseif` `` marker ([Block Controls](./Block%20Controls.md)) | `condition` on the `branch` edge | The next branch, or the `` `else` ``, is tried | `` > `if` `Rich?` `` |

The attach points differ because the constructs differ: a jump can sit mid-line,
so its condition travels with it through the inline stream, while a line and an
option always start their block, so their condition is read off there.

## Conditional jump

```markdown
`FoundKey?` => [Open the vault](#the-vault)
=> [Search the study](#the-study)
```

If `FoundKey` is true the reader takes the vault; otherwise the jump is skipped
and the unconditional jump to the study runs. The condition must sit on the same
line, immediately before `=>`; spaces between them are allowed. A conditional
jump inherits every other rule of a [jump](../../../guide/structure-and-flow.md#jumps).

## Conditional line

```markdown
`Angry?` Guard: You again? Get out.
`NotAngry?` Guard: Back so soon? Go on through.
```

The condition sits **before the speaker**, and `Guard` is still recognized as the
speaker. A speaker-less line may be conditional too (`` `Returned?` Welcome back. ``).

`LineBuilder` reads off the leading condition only when non-jump content follows it:

- when a `=>` follows, the condition is left for the jump to claim;
- when nothing follows, the condition is left in place and reported as guarding
  nothing (`DLG1106`).

## Conditional choice option

```markdown
- `IsAngry?` `50%` The guard glares and blocks your path.
- `30%` The guard waves you through.
- `20%` The guard ignores you.
```

The condition comes **first, before the weight**. `RandomChoiceRecognition`
peeks past it to find the weight, so a condition-first option still makes the
list a random choice. When `IsAngry` is false the first option is excluded and
30 and 20 re-normalize to 60% and 40%.

The option condition is read off at the **list item**, before the body is built,
so it guards the whole option and takes precedence over the inner line and jump
handling:

| Option written | Reads as |
| --- | --- |
| ``- `c?` Bob: Attack`` | a conditional **option**; its body line is unconditional |
| ``- `c?` => [x](#x)`` | a conditional **option** whose body is a plain jump |
| ``- `50%` `c?` Bob: Attack`` | a random option whose body **line** is conditional |

## Resolution

The contract every runtime honors, for every attach point:

1. The runtime reads the key from the world as a boolean.
2. `true` lets the construct happen; `false` applies the construct's false
   behavior from [the table above](#where-a-condition-attaches).
3. An unknown key is `false`, so a flag that was never set does not fire.

No host interface for reading the world ships yet: the runner asks its driver
with `Resolve`. A dedicated boolean read is part of the world interface proposed in
the [runtime architecture](../runtime/Dialogue%20Runtime%20Architecture.md#reading-the-world),
and its name is not settled.

## Key design decisions

### D1 — A condition is a read, not a command

A condition reads game state, so it belongs with queries rather than with effects.
A command form such as `` `If("Rainy")` `` would borrow the command grammar for a
read, reserve `If` out of the game's command names, and invite an expression
language.

### D2 — The `?` sigil joins the query-and-sigil family

A writer who knows `` `"key"` `` and `` `key%` `` already knows the shape. The
sigil after the key is the operator, and quotes escape a key that ends in one.

### D3 — Guard-first placement

Written before what it guards, a condition reads "if … then …" and is scannable
at the start of the construct. One placement rule serves every attach point,
matching Ink's `{cond} …`. Placing it after (``Guard: `Angry?` Leave.``) buries
it mid-line.

### D4 — A dedicated boolean read

A condition resolves through a boolean read of its key, so the runtime never
parses `"true"` out of a string and there is no truthiness ladder. Dynamic
weights still read a value, because a number in a string is natural where a
boolean is not.

### D5 — One spanned, reusable node

`Condition` is its own spanned `ScriptNode`, and every guarded construct holds it
through the `IConditional` interface (`Line`, `ControlLine`, `Jump`, `Choice`,
`RandomOption`, `Branch`). Tooling can point at the exact condition, and one node
and one reader serve every attach point.

### D6 — An option condition is read off at the list item and wins

A condition on a menu item is meant to guard the menu item, so the list item's
condition is read off before the inner builders run, and they never bind it again
(see the precedence table above). The line and the option share
`ConditionReader.TryReadLeading`; each applies its own binding policy.

### D7 — False falls through; no inline else

A condition guards exactly one construct. The alternative is written on the next
line, often as a condition on an inverse flag. Grouped, mutually exclusive
branches with a fallback are the separate [block control](./Block%20Controls.md).

### D8 — A conditional random option defers the weight total

A conditional option may be excluded at play time, so the achievable total is
unknown at compile time. `WeightTotalRule` skips `DLG3003` and `DLG2010` for a
random choice with any conditional option, exactly as it does for a dynamic
weight. A conditional option still needs a weight (`DLG1104`).

### D9 — A player option is shown unavailable, not removed

A false player option is reported as unavailable in the menu rather than dropped,
so the host decides whether to hide or disable it. The runtime architecture owns
this decision
([D8 there](../runtime/Dialogue%20Runtime%20Architecture.md#d8--a-menu-shows-unavailable-options)),
and the `an-unavailable-option` conformance case pins it.

### D10 — No negation, no expressions

There is no `not`, `and`, or comparison. "Unless" is a game-defined inverse flag
(`` `NotRainy?` ``), and the game composes logic behind one key. A prefix `!`
was rejected as cryptic for non-technical writers and can be added later without
changing `?`.

## Diagnostics

| Code | Meaning | Severity | When |
| --- | --- | --- | --- |
| `DLG1106` | A condition guards nothing | Error | The condition is not bound: not immediately before a `=>`, not at the start of a line or option with content, and not after an `` `if` `` / `` `elseif` `` marker. |

A code span that is not a clean condition falls back to game-call recognition,
and if that fails it is `DLG1102` and kept as literal text. There is no
invalid-value diagnostic: a condition always resolves to true or false.

## Error and boundary cases

| Input | Result |
| --- | --- |
| `` `K?` => [L](#a) `` | Conditional jump. |
| `` `K?` Guard: Hi `` | Conditional line; `Guard` is the speaker. |
| `` `K?` Hello `` | Conditional line with the default speaker. |
| `` `K?` `` alone on a line | `DLG1106`. |
| ``Guard: You `K?` there`` | `DLG1106`; a condition inside speech guards nothing. |
| `` `"Rainy?"?` Guard: Hi `` | The key is `Rainy?`. |
| `` `"a" "b"?` Guard: Hi `` | Not a condition; `DLG1102`, literal text, the line is unguarded. |
| `` - `K?` `50%` … `` | Conditional random option with both a condition and a weight. |
| Every option in a random choice conditional | Accepted; the weight total is deferred, and an all-false pool selects nothing at play time. |
| A condition in a heading | Read as heading text; a heading cannot hold a jump. |

## Deferred work

- **A condition on a choice option.** It is compiled and carried into the
  playbook, and the runner evaluates it once it plays choices.
- **Negation and expressions.** Deferred by [D10](#d10--no-negation-no-expressions).

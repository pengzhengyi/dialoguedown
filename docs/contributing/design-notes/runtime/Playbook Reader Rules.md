# Playbook Reader Rules

> [!NOTE]
> Status: **implemented**. Three reader rules close gaps where two conformant
> runtimes could otherwise play one playbook differently: every node's ways out have
> a well-defined shape (`OutwardShapeChecker`), a `branch`'s else comes after every
> gated arm (`BranchArmOrderChecker`), and a `random-choice`'s number weights can be
> drawn against (`NumberWeightChecker`). All three run in the default reader, the
> schema states what it can of each, and `readable/` conformance cases pin the
> refusals. The reader pipeline they join is in
> [Playbook Format](./Playbook%20Format.md#reading-a-playbook).

## Table of contents

- [Ubiquitous language](#ubiquitous-language)
- [Outward shape](#outward-shape)
- [Branch arm order](#branch-arm-order)
- [Number weights](#number-weights)
- [Key design decisions](#key-design-decisions)
- [Error and boundary cases](#error-and-boundary-cases)
- [Testability](#testability)
- [Open questions](#open-questions)

## Ubiquitous language

| Term | Meaning |
| --- | --- |
| **Outward shape** | The kinds and counts of edges a node's `out` may hold. |
| **Arm** | An edge a node chooses among to move forward: a `divert` for `line` and `control`, an `option` for `choice`, a `random-option` for `random-choice`, a `branch` for `branch`. Each node kind offers one arm kind; `end` offers none. |
| **Gated arm** | An arm carrying a `condition`. |
| **Open arm** | An arm with no condition — it always applies. |
| **Else** | A `branch` arm with no condition. |
| **Fall-through** | The single `succession` edge a node takes when no arm applies. |
| **Leads somewhere** | Following `out` always reaches a next node, whatever the conditions decide. |
| **Arm order** | The sequence a `branch` node's arms are tried in: the order they appear in its `out`, first to last. |

## Outward shape

> A non-`end` node's `out` holds its **arms** — every edge of the one arm kind its
> node kind offers — and **at most one `succession` fall-through**. The
> fall-through is needed when the node has no open arm, or is itself conditional.
> `end` carries no edges.

The property this buys is **every node leads somewhere**. The per-kind table is the
consequence of the rule, and `NodeShape.For` holds it as data:

| Node kind | Arm kind | Arm count | A fall-through is needed when |
| --- | --- | --- | --- |
| `line` | `divert` | 0–1 | the node is conditional, its divert is gated, or it has no divert |
| `control` | `divert` | 0–1 | as `line` |
| `choice` | `option` | ≥ 1 | every option is gated |
| `random-choice` | `random-option` | ≥ 1 | every random-option is gated |
| `branch` | `branch` | ≥ 1 | every branch arm is gated (there is no else) |
| `end` | — | 0 | never |

A needed fall-through that is absent means the node leads nowhere: refused. A
fall-through that is present but not needed is dead: accepted
([D3](#d3--a-dead-fall-through-is-accepted)). A node with zero arms is vacuously
"every arm gated", so a plain line still needs its fall-through.

## Branch arm order

> A `branch` node's arms are tried in the order they appear in its `out`. At least
> one of them is gated, and the else, when present, is the last arm.

Two independent guards, both required:

- at least one arm is gated, so the else has something to fall back from;
- the else is last, so it is tried only after every gated arm. Because only one arm
  can be last, a second else is refused too.

Together they keep a branch meaning what its chain says. An else is taken whenever
it is reached, so an else before another arm would leave that arm unreachable. A
`succession` may sit anywhere among the arms, because it is not an arm.

## Number weights

> A `random-choice` node's number weights are finite and not negative, and they add
> up to a finite total.

A draw is made against the total of the arms' shares, so a weight of `-10`, or one
of `1e400`, which reads as infinite, would leave the draw without meaning, and
numbers that are each finite but add up past what a double holds would too. The
compiler never writes any of these, but a hand-built playbook can.

Checking them on reading lets a runtime trust every number a playbook carries, and
leaves only the numbers the world supplies for key weights to check at play time. A
guarded arm's number is checked like any other, because a guard decides whether an
arm is drawn, not what its weight means.

## Key design decisions

### D1 — State the invariant, not the table

The rule is one sentence over three concepts — arm, gated, fall-through — so a
refusal is explained as "this node has no way on" rather than by reciting a row. A
new node kind is a new `NodeShape.For` row plus a failing exhaustiveness test until
it is written.

### D2 — The reader is the authority; the schema helps where it can

A schema describes shape in isolation. It can restrict each node kind's `out` to its
edge kinds (`items`), demand an arm (`contains`), bound the succession count
(`minContains` / `maxContains`), demand a gated `branch` arm, and allow at most one
else. It cannot relate a succession's presence to the conditions on other edges, and
it cannot compare positions, so "leads somewhere" and "last" belong to the
reader. For a number weight it states `minimum: 0`, but a number too large for a
double is still a valid JSON number, and the schema cannot add the numbers up, so
"finite" belongs to the reader too.

```json
"allOf": [
  { "contains": { "properties": { "kind": { "const": "branch" } }, "required": ["condition"] } },
  { "contains": { "properties": { "kind": { "const": "branch" }, "condition": false } },
    "minContains": 0, "maxContains": 1 }
]
```

`contains` defaults `minContains` to `1`, so the else clause needs `minContains: 0`
or every else-less branch would fail. Because the `node` schema is a `oneOf`,
`check-jsonschema` also reports a node failing every *other* kind, and its "Best
Match" line may name the wrong cause; the full error list names the real one.

### D3 — A dead fall-through is accepted

A fall-through beside an open arm can never run, but it plays identically, so
refusing it would reject a harmless document. `IPlaybookChecker` is throw-or-silent,
so it is accepted. Warning about it needs a diagnostics channel on the reader, which
does not exist.

### D4 — The array is the order

An arm's place in the order is its position in `out`. JSON keeps the order of an
array, so the position already says everything a separate `order` field could, and
a second copy could only disagree with it. A port keeps `out` in the order it was
read; one that reorders the arms fails the `a-conditional-block` playable case,
which takes the first arm over the else. The compiler writes the arms in source
order, so every compiled branch already satisfies the rule.

### D5 — One idea per checker, in dependency order

`PlaybookCheckerFactory.CreateDefault()` runs `ReferenceChecker`, then
`OutwardShapeChecker`, then `BranchArmOrderChecker`, then `NumberWeightChecker`. Each
can then assume what the one before it proved: edges land on real nodes, and a
branch carries at least one arm of the right kind. `NumberWeightChecker` reads only
the weights and assumes nothing.

## Error and boundary cases

| Case | Behavior | Corpus case |
| --- | --- | --- |
| `line` carrying an `option`, `branch`, or `random-option` | refuse — foreign arm kind | `a-foreign-way-out` |
| any node with two `succession` edges | refuse | `two-fall-throughs` |
| a node needing a fall-through with none (a line with no edges, an all-gated choice, an `if` with no else, a conditional line) | refuse — leads nowhere | `no-way-onward` (a line with no edges) |
| `line` / `control` with two `divert` edges | refuse | — |
| `choice` / `random-choice` / `branch` with no arm | refuse | — |
| node with an open arm **and** a `succession` | accept — the succession is dead | — |
| `end` with an edge | cannot occur — `EndNode` takes no edges and the reader drops an `out`; the schema's `maxItems: 0` is the backstop | — |
| else first, a gated arm after | refuse — else not last | `else-arm-not-last` |
| two else arms | refuse | `two-else-arms` |
| a branch whose only arm is an else | refuse — nothing to fall back from | `an-else-alone` |
| a `succession` before or among the branch arms | accept — not an arm | — |
| a number weight that is negative, or reads as infinite (`1e400`) | refuse | `weight-is-negative`, `weight-is-infinite` |
| number weights that are each finite but add up past what a double holds | refuse | `weight-total-is-infinite` |
| a guarded arm's negative number weight | refuse — a guard does not excuse a weight | `guarded-weight-is-negative` |

## Testability

| Layer | Test |
| --- | --- |
| Unit | `OutwardShapeCheckerTests`, `BranchArmOrderCheckerTests`, and `NumberWeightCheckerTests`: an accept and a refuse per rule, each refusal breaking one thing in a well-formed node |
| Exhaustiveness | Exactly six concrete `Node` kinds, so a seventh forces a `NodeShape.For` row |
| Property | The playbook round-trip property: every compiled script's playbook passes all three checkers |
| Cross-runtime | The `readable/` cases in the table above |
| Schema | CI validates every golden and accepted conformance playbook; the goldens' else-less branches protect `minContains: 0` |

The round-trip property catches a checker that is too strict, never one too lax. A
writer assertion that each emitted branch puts its else last would guard the other
direction.

## Open questions

- **A diagnostics channel on the reader** for valid-but-suspect documents, with the
  dead fall-through as its first case.
- **A way to withhold an arm other than `condition`.** "Gated" reads conditions
  straight off the edges; a future capability that withholds an arm some other way
  would need this rule revisited.

# Playbook Reader Rules

> [!NOTE]
> Status: **implemented**. Two reader rules close gaps where two conformant
> runtimes could otherwise play one playbook differently: every node's ways out have
> a well-defined shape (`OutwardShapeChecker`), and a `branch`'s arms are in one
> canonical order (`BranchArmOrderChecker`). Both run in the default reader, the
> schema states what it can of each, and `readable/` conformance cases pin the
> refusals. The reader pipeline they join is in
> [Playbook Format](./Playbook%20Format.md#reading-a-playbook).

## Table of contents

- [Ubiquitous language](#ubiquitous-language)
- [Outward shape](#outward-shape)
- [Branch arm order](#branch-arm-order)
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
| **Arm order** | The `order` a `branch` arm carries: where it sits in the sequence the arms are tried, lowest first. |

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

> A `branch` node's arms appear in its `out` in strictly ascending `order`, at
> least one of them is gated, and the else, when present, is the last arm.

Three independent guards, all required:

- at least one arm is gated, so the else has something to fall back from;
- the arms ascend, so the order in the array is the order the `order` fields state;
- the else is last, so it is tried only after every gated arm. Because only one arm
  can be last, a second else is refused too.

Together they buy **agreement**: a reader that walks `out` and one that sorts by
`order` take the same arm. Gaps (`0`, `2`) are allowed; a `succession` may sit
anywhere among the arms, because it is not an arm.

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
it cannot compare positions or values, so "leads somewhere", "ascending", and
"last" belong to the reader.

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

### D4 — `order` is authoritative; the sorted array is canonical

`order` exists because a port may reorder, normalize, or model `out` as an
unordered collection. The checker makes the array and `order` agree, so a reader
that ignores `order` still takes the right arm. The compiler emits `order` as the
arm's source index, so every compiled branch already satisfies the rule.

### D5 — One idea per checker, in dependency order

`PlaybookCheckerFactory.CreateDefault()` runs `ReferenceChecker`, then
`OutwardShapeChecker`, then `BranchArmOrderChecker`. Each can then assume what the
one before it proved: edges land on real nodes, and a branch carries at least one arm
of the right kind.

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
| branch arms `order` `1` then `0` | refuse — not ascending | `arms-out-of-order` |
| two branch arms sharing an `order` | refuse — ascending is strict | `arms-share-an-order` |
| else first, a gated arm after | refuse — else not last | `else-arm-not-last` |
| two else arms | refuse | `two-else-arms` |
| a branch whose only arm is an else | refuse — nothing to fall back from | `an-else-alone` |
| branch arms `order` `0`, `2` | accept — a gap is not a fault | — |
| a `succession` before or among the branch arms | accept — not an arm | — |

## Testability

| Layer | Test |
| --- | --- |
| Unit | `OutwardShapeCheckerTests` and `BranchArmOrderCheckerTests`: an accept and a refuse per rule, each refusal breaking one thing in a well-formed node |
| Exhaustiveness | Exactly six concrete `Node` kinds, so a seventh forces a `NodeShape.For` row |
| Property | The playbook round-trip property: every compiled script's playbook passes both checkers |
| Cross-runtime | The `readable/` cases in the table above |
| Schema | CI validates every golden and accepted conformance playbook; the goldens' else-less branches protect `minContains: 0` |

The round-trip property catches a checker that is too strict, never one too lax. A
writer assertion that each emitted branch is sorted would guard the other direction.

## Open questions

- **A diagnostics channel on the reader** for valid-but-suspect documents, with the
  dead fall-through as its first case.
- **A way to withhold an arm other than `condition`.** "Gated" reads conditions
  straight off the edges; a future capability that withholds an arm some other way
  would need this rule revisited.

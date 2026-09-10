# Node outward shape

> [!NOTE]
> Status: **implemented**. Every node kind has a well-defined set of ways out;
> before this the compiler upheld the shape, the format did not describe it, and
> the reader did not check it. `OutwardShapeChecker` now checks it in the default
> reader, the schema constrains it as far as it reaches, and the conformance
> corpus pins the refusals. This note assumes the vocabulary of the
> [Playbook format](./Playbook%20Format.md) — *playbook*, *reader*, *checker*,
> *node*, *edge* — and does not restate it.

## Table of contents

- [Goal and scope](#goal-and-scope)
- [Ubiquitous language](#ubiquitous-language)
- [The rule](#the-rule)
- [Functionality checklist](#functionality-checklist)
- [Interfaces and abstractions](#interfaces-and-abstractions)
- [Key design decisions](#key-design-decisions)
- [Error and boundary cases](#error-and-boundary-cases)
- [Integration](#integration)
- [Testability](#testability)
- [Open questions](#open-questions)

## Goal and scope

A hand-edited playbook, or one written by another tool, can carry a line with two
successions, an option out of an `end`, or a fully gated choice with nowhere to
go when every option is withheld — and before this the reader loaded all of them.
A runtime then plays whatever the edge array happens to list first, a silent
disagreement between two conformant runtimes: exactly what the
[conformance corpus](./Conformance%20Corpus.md) exists to prevent.

In scope:

- a **reader rule** — each node carries only the edge kinds it can act on, in the
  counts it can act on, and always leads somewhere;
- **schema constraints** where JSON Schema 2020-12 can express them;
- **conformance `readable/` cases** for the refusals that result, built from
  `baseline` with one field changed, as every other case is.

Three pieces are deliberately left out, each tracked as its own follow-up:

- **The runner's own tightening.** The traversal code reads a node's succession
  with `FirstOrDefault` rather than `Single`, because nothing guarantees there is
  only one. Now this checker is in the default reader, a playbook that reaches
  the runner has at most one, and that call can tighten. The change belongs to
  the runner's code — made from here it would only cause a merge conflict.
- **Edge ordering.** A `branch` arm's `order` should ascend, and the `else` arm
  should come last. That is a separate invariant about the sequence of arms, not
  their shape.
- **A diagnostics channel on the reader** for valid-but-suspect documents — see
  [decision 3](#3-a-fall-through-that-cannot-run).

## Ubiquitous language

| Term | Meaning |
| --- | --- |
| **Outward shape** | The kinds and counts of edges a node's `out` may hold. |
| **Arm** | An edge the node chooses among to move forward: a `divert` for `line` and `control`, an `option` for `choice`, a `random-option` for `random-choice`, a `branch` for `branch`. Each node kind offers exactly one arm kind; `end` offers none. |
| **Gated arm** | An arm carrying a `condition`. A `branch` arm with no condition is the `else` arm and is never gated. |
| **Open arm** | An arm with no condition — one that always applies. |
| **Fall-through** | The single `succession` edge a node takes when no arm applies. |
| **Leads somewhere** | Following `out` always reaches a next node — an arm whose condition holds, or the fall-through. A node that leads nowhere is refused. |

## The rule

> A non-`end` node's `out` holds its **arms** — every edge of the one arm kind
> its node kind offers — and **at most one `succession` fall-through**. The
> fall-through is there when the node has no open arm, or is itself conditional:
> exactly the cases where an arm might not apply, and the node would otherwise
> lead nowhere. `end` carries no edges at all.

The property this buys is **every node leads somewhere**: whatever the conditions
decide at play time, `out` always has a next step. A node that could reach a dead
end is refused, and so is a node carrying an edge kind it cannot act on — that
means something the writer did not say.

A fall-through that *can* never run — the node has an open arm — is neither
required nor wrong; it plays identically. The checker accepts it, for the reasons
in [decision 3](#3-a-fall-through-that-cannot-run).

The per-kind table is the *consequence* of the rule, not the specification:

| Node kind | Arm kind | Arm count | A fall-through is needed when |
| --- | --- | --- | --- |
| `line` | `divert` | 0–1 | the node is conditional, its divert is gated, or it has no divert |
| `control` | `divert` | 0–1 | as `line` |
| `choice` | `option` | ≥ 1 | every option is gated |
| `random-choice` | `random-option` | ≥ 1 | every random-option is gated |
| `branch` | `branch` | ≥ 1 | every branch arm is gated (there is no `else`) |
| `end` | — | 0 | never — `end` carries no edges |

A needed fall-through that is **absent** means the node leads nowhere: refused. A
fall-through that is **present but not needed** is dead: accepted (decision 3).

"Gated" is read straight from the document: an `option`, `random-option`,
`branch`, or `divert` edge is gated exactly when it carries a `condition`; a
`line` or `control` node is conditional exactly when it carries one. A node with
zero arms is vacuously "every arm gated", so a plain line still needs its
fall-through.

## Functionality checklist

- [x] Refuse a node carrying an edge kind its node kind cannot act on.
- [x] Refuse a node with more than one `succession`.
- [x] Refuse a `choice`, `random-choice`, or `branch` with no arm.
- [x] Refuse a `line` or `control` with more than one `divert`.
- [x] Refuse a non-`end` node that needs a fall-through and has none — a node
      that would lead nowhere.
- [x] Handle `end`: an `end` with edges is impossible to build, and the reader
      drops an `out` it finds on one, so the checker returns early and the
      schema's `maxItems: 0` is the backstop.
- [x] **Accept** a node with a fall-through it does not need
      ([decision 3](#3-a-fall-through-that-cannot-run)).
- [x] Schema constraints on each node kind's `out`: the allowed edge kinds via
      `items`, a `contains` for the arm minimum, and `minContains` /
      `maxContains` for the succession.
- [x] Conformance `readable/` cases covering the new refusals, from `baseline`.
- [x] The checker wired into `PlaybookCheckerFactory.CreateDefault()`.

## Interfaces and abstractions

| Type | Responsibility | Collaborators |
| --- | --- | --- |
| `OutwardShapeChecker : IPlaybookChecker` | Walk the nodes; refuse the first whose `out` breaks [the rule](#the-rule). Three guards: a foreign arm kind, an arm count out of range, a bad fall-through. | `PlaybookDocument`, the `Node` / `Edge` model, `NodeShape`, `InvalidPlaybookException` |
| `NodeShape` (record struct) | The static half of the rule as data — arm edge type, arm-count bounds, and whether this node is conditional. `NodeShape.For(node)` is the switch that reads a node's row. | `OutwardShapeChecker` |
| `PlaybookCheckerFactory` | Adds `OutwardShapeChecker` to `CreateDefault()`, after `ReferenceChecker`. | `CompositeChecker` |
| `schema/playbook-0.schema.json` | Per-kind `out` definitions: `items` restricts the edge kinds, a `contains` sets the arm minimum, `minContains` / `maxContains` bound the succession count. `end` keeps `maxItems: 0`. | `check-jsonschema` in CI |
| `conformance/readable/<case>/` | One refusal each: `playbook.json` is `baseline` with one field changed, `fixture.json` verdict `refuse`. | the readable harness |

The checker follows the house contract: it refuses at the first fault with a
message naming the offending node and what was expected — a playbook is compiler
output, so there is no list for a reader to work through.

## Key design decisions

### 1. State the invariant, not the table

The design names three concepts — **arm**, **gated**, **fall-through** — and the
rule is one sentence over them; the per-kind table falls out. Naming the property
(**every node leads somewhere**) is what lets a refusal be explained in words a
writer understands — "this node has no way on" — rather than by reciting a row.
The table itself lives in the code as data: `NodeShape.For` is a switch whose
arms line up as columns, so a new node kind is a new row plus a failing
exhaustiveness test until it is written.

### 2. The reader is the authority; the schema helps where it can

Same split as `ReferenceChecker`. A schema describes shape in isolation: it can
say which edge kinds an array may hold and cap how many of one kind, so it
catches a second `succession`, a missing arm, and a foreign arm kind. It cannot
relate the *presence* of a `succession` to the *conditions* on the other edges,
which is the "leads somewhere" half. So the reader states the whole rule and the
schema mirrors the part it can — the split the rest of the corpus already shows,
where most `readable/` refusals are valid by the schema and the reader is what
makes them refusals.

```mermaid
flowchart LR
    D["playbook.json"] --> S["JSON Schema<br/>multiplicity, arm kind"]
    D --> R["OutwardShapeChecker<br/>the whole rule"]
    S -. "necessary, not sufficient" .-> R
```

One cost of the per-kind `out` definitions: the `node` schema is a `oneOf`, so
when a node fails its own kind's `out` rule `check-jsonschema` also reports it
failing every *other* kind, and its "Best Match" line picks one of those —
usually a missing field like `effects`. The full error list names the real
cause. This is serviceable because the reader, not the schema, is the authority.

### 3. A fall-through that cannot run

Whether a `succession` is doing work is fully structural: it runs only when no
arm applies, which can happen only if the node has no open arm or is itself
conditional. So the checker can always tell a working fall-through from a dead
one.

A dead fall-through — the node has an open arm, so `out` already always has a
next step — is not wrong. It plays identically with or without it. Refusing it
would reject a harmless document and frustrate the hand-editors and tools the
format is meant to welcome. But silently accepting it throws away a real signal:
a dead `succession` usually means a hand-edit slip or a writer over-emitting.

`IPlaybookChecker` is throw-or-silent — there is no third answer — so the checker
**accepts** the dead fall-through. Turning it into a *warning* instead needs a
diagnostics channel on the reader, parallel to the compiler's, and something to
surface it through (a play command that reads a playbook before running it). That
is its own component, tracked as a follow-up, with the dead fall-through as its
first case.

### 4. Checker order: after `ReferenceChecker`

`ReferenceChecker` proves every edge lands on a node the document holds. Running
after it, this checker reasons about edges already known to be sound and never
has to guess whether a dangling edge is also a shape error.

## Error and boundary cases

| Case | Behaviour |
| --- | --- |
| `end` with any edge | cannot occur — the `EndNode` record takes no edges and the reader drops an `out` it reads on one; the schema's `maxItems: 0` is the backstop |
| `line` / `control` with two or more `divert` edges | refuse |
| `line` carrying an `option`, `branch`, or `random-option` | refuse (foreign arm kind) |
| `choice` carrying both an `option` and a `branch` edge | refuse (foreign arm kind — a choice's arms are options) |
| `choice` / `random-choice` / `branch` with no arm | refuse |
| any node with two or more `succession` edges | refuse |
| all-gated `choice`, `if` with no `else`, conditional `line`, `line` with a gated `divert` — and no `succession` | refuse (leads nowhere) |
| non-`end` node with an empty `out` | refuse — zero arms is vacuously "every arm gated", so a fall-through is needed |
| node with an open arm **and** a `succession` | accept — the `succession` is dead ([decision 3](#3-a-fall-through-that-cannot-run)) |
| `branch` whose `else` arm is a conditionless `branch` edge | that arm is open, so the fall-through is not needed |

## Integration

- **`PlaybookReader.Default`** gains the checker through
  `PlaybookCheckerFactory.CreateDefault()`. Every runtime built on the default
  reader refuses these documents from then on.
- **The playbook round-trip property** — that everything the writer produces the
  reader accepts — is now a free guard that the checker never rejects valid
  compiler output. The script generator behind that property was taught, just
  before this, to emit the three hard outward shapes (a line that diverts, an
  all-gated choice, an `if` with no `else`), so the guard exercises them rather
  than passing hollow.
- **The conformance corpus** gains three `readable/` cases, so a port that fails
  to refuse a two-succession line fails conformance.

## Testability

| Layer | Test |
| --- | --- |
| Unit | `OutwardShapeCheckerTests` — an accept and a refuse for each row of the rule; each refuse starts from a well-shaped node and breaks one thing. Nodes are built through the shared test factory. |
| Exhaustiveness | A test asserts there are exactly six concrete `Node` kinds, so a seventh forces a `NodeShape.For` row and its own cases. |
| Property | Wired into the round-trip property: every compiled script's playbook passes the checker. |
| Cross-runtime | Three `readable/` cases — a foreign arm kind, two successions, a node with no way out. |
| Schema | CI validates every golden and every accepted conformance playbook against the schema; the per-kind constraints were checked by hand against each expressible violation. |

Mutation testing of the checker — small, pure, boundary-heavy, a good target — is
left to the project's separate mutation-testing effort.

## Open questions

1. **Does "gated" need to account for a condition the checker cannot see?** The
   definition here reads conditions straight off the edges and nodes. If a future
   capability introduces a way to withhold an arm that is not a `condition`
   field, this rule would need revisiting — flagged, not expected.

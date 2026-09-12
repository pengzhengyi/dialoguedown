# Branch arm order

> [!NOTE]
> Status: **proposed**. A `branch` node's arms carry an explicit `order` and a
> conditionless `else`; this note adds a reader rule that the arms appear in
> ascending order with the `else` last, and mirrors the part of it the schema can
> express. It assumes the vocabulary of the
> [Playbook format](./Playbook%20Format.md) and the
> [Node outward shape](./Node%20Outward%20Shape.md) note — *playbook*, *reader*,
> *checker*, *node*, *edge*, *arm*, *gated*, *open*, *else*, *fall-through* — and
> does not restate it.

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

The [outward-shape checker](./Node%20Outward%20Shape.md) guarantees *which* edge
kinds a node carries and *how many*; it deliberately says nothing about the order
of a `branch`'s arms. But the arms are tried in that order, and each carries an
explicit `order` so the meaning does not depend on a reader preserving the array
it was written in: a port may reorder, normalize, or model `out` as an unordered
collection. When the arms are listed out of order, or the `else` arm does not come
last, two conformant readers — one that trusts the array, one that sorts by
`order` — can take different arms: a silent disagreement, exactly what the
[conformance corpus](./Conformance%20Corpus.md) exists to prevent.

In scope:

- a **reader rule** — a `branch`'s arms appear in its `out` in strictly ascending
  `order`, and the conditionless `else` arm, when present, is the last arm;
- the **schema constraint** where JSON Schema 2020-12 reaches — at most one
  conditionless `branch` arm;
- **conformance `readable/` cases** for the refusals that result.

Two pieces are deliberately left out, each tracked as its own follow-up, as the
outward-shape note did:

- **The runner's own tightening.** The traversal can assume a branch's arms are in
  order once the reader enforces it; that change belongs to the runner's code.
- **A diagnostics channel on the reader** for valid-but-suspect documents, whose
  first case remains the dead fall-through named in the outward-shape note.

## Ubiquitous language

The rule reuses the outward-shape note's terms — **arm**, **gated arm**, **open
arm**, **fall-through**, and **else** ("a `branch` arm with no condition is the
`else` arm") — and adds two:

| Term | Meaning |
| --- | --- |
| **Arm order** | The `order` a `branch` arm carries: where it sits in the sequence they are tried, lowest first. |
| **Ascending arms** | Arms whose `order` strictly increases as they appear in `out`. |

## The rule

> A `branch` node's arms appear in its `out` in strictly ascending `order`, and a
> conditionless `branch` arm — the `else` — is the last arm.

The rule is **two independent guards**, and a document must satisfy both:

- the arms ascend, so the order a reader sees in the array is the order the
  `order` fields state;
- the `else`, when present, is last, so it is tried only after every gated arm.

Together they buy **agreement**: a reader that walks `out` and one that sorts by
`order` take the same arm, so the document cannot mean two things. One point of
overlap is worth naming: because at most one arm can be last, "the `else` is last"
also means a second conditionless arm is refused.

## Functionality checklist

- [ ] Refuse a `branch` whose arms are not in strictly ascending `order`.
- [ ] Refuse a `branch` whose `else` arm is not the last arm.
- [ ] Refuse a `branch` with more than one conditionless arm (the earlier is not
      last).
- [ ] Refuse a `branch` whose `else` is last but whose `order`s do not ascend.
- [ ] Accept a `branch` with a single arm — nothing to order.
- [ ] Accept a `branch` whose `order`s have gaps (for example `0`, `2`).
- [ ] Accept a `succession` fall-through wherever it sits among the arms.
- [ ] Ignore non-`branch` nodes, whatever their edges.
- [ ] Schema: `branchOut` allows at most one conditionless `branch` arm, and still
      allows none.
- [ ] Conformance `readable/` cases covering the new refusals.
- [ ] The checker wired into `PlaybookCheckerFactory.CreateDefault()`.

## Interfaces and abstractions

| Type | Responsibility | Collaborators |
| --- | --- | --- |
| `BranchArmOrderChecker : IPlaybookChecker` | Walk the nodes; for each `branch`, refuse the first whose arms are out of order or whose `else` is not last. Two guards: ascending `order`, else last. | `PlaybookDocument`, the `Node` / `BranchEdge` model, `InvalidPlaybookException` |
| `PlaybookCheckerFactory` | Adds `BranchArmOrderChecker` to `CreateDefault()`, after `OutwardShapeChecker`. | `CompositeChecker` |
| `schema/playbook-0.schema.json` | `branchOut` gains a `contains` capping conditionless `branch` arms at one — `minContains: 0`, `maxContains: 1`. | `check-jsonschema` in CI |
| `conformance/readable/<case>/` | One refusal each: an accepted compiled source and its `playbook.json` with one field changed, verdict `refuse`. | the readable harness |

The checker follows the house contract: it refuses at the first fault with a
message naming the offending node and what was expected — a playbook is compiler
output, so there is no list for a reader to work through.

## Key design decisions

### 1. The `order` field is authoritative; the sorted array is the canonical form

The `order` field exists because a reader may not preserve `out`'s sequence. So
`order` carries the precedence, and the array is a second, redundant reading. The
checker makes the two agree: the arms must ascend as written, and the `else` must
be last. That gives a document a single canonical form, so even a reader that
ignores `order` and walks `out` reaches the right arm. Gaps are allowed — the
compiler emits `0`, `1`, …, but the sequence is what matters, not that the numbers
are dense — and `order` is kept rather than declared redundant because a port is
free to reorder the array it was handed.

### 2. `else` is last among the arms, not last in `out`

A `branch`'s `out` also holds at most one `succession` fall-through, which is not
an arm. The rule orders the arms, so the fall-through may sit anywhere among them
and is left unconstrained; ordering it would be a second invariant about a
different thing, and the outward-shape note already governs how many there are. A
reader that walks `out` linearly and takes the first applicable edge would still be
non-conformant if a fall-through led the arms — but that reader is wrong for
reasons this rule does not own.

### 3. A separate checker, after the shape checker

The outward-shape note left this invariant out on purpose — it is about the
*sequence* of arms, not their shape. Keeping it in its own checker keeps each
checker one idea and its tests focused. Running it after `OutwardShapeChecker`
means the branch it examines is already known to carry at least one arm and only
arms of the right kind, so it never has to guess whether a malformed edge is also
an ordering error.

### 4. The reader is the authority; the schema helps where it can

JSON Schema 2020-12 cannot compare positions or values, so it cannot express
"ascending" or "last". What it can express is a count: how many conditionless
`branch` arms an array contains, capped at one. That single constraint mirrors the
`else` half of the rule; the ascending half, and the whole rule together, belong to
the reader. This is the same split the outward-shape work draws, where most
`readable/` refusals are valid by the schema and the reader is what makes them
refusals.

The clause is easy to get subtly wrong: `contains` defaults `minContains` to `1`,
so `minContains: 0` is required or the schema would demand exactly one conditionless
arm and reject every else-less branch the compiler emits. Matching "no condition"
uses `"condition": false` on the sub-schema — a property that must be absent.

```json
{
    "contains": {
        "properties": { "kind": { "const": "branch" }, "condition": false }
    },
    "minContains": 0,
    "maxContains": 1
}
```

### 5. A richer conformance case ships its own accepted source

`readable/baseline/` holds only a `line` and an `end`, so no one-field edit of it
can produce a branch. The ordering refusals therefore ship their own compiled
`source.dialogue.md` — an accepted branch document — and change one field, exactly
as the corpus's [Adding a case](../../../../conformance/README.md) process already
describes. The corpus README's "every refusal is `baseline` with one field changed"
story is narrowed accordingly: a refusal is *an accepted document* with one field
changed, and the simple line-level cases happen to share `baseline/`.

## Error and boundary cases

| Case | Behaviour |
| --- | --- |
| `branch` with one arm | accept — nothing to order |
| `branch` with no arm | cannot reach here — the outward-shape checker refuses it first, and this checker returns early on fewer than two arms |
| arms `order` `0`, `1`, `2`, else last | accept |
| arms `order` `0`, `2` | accept — a gap, not a fault |
| arms `order` `1` then `0` | refuse — not ascending |
| two arms sharing an `order` | refuse — ascending is strict |
| else arm first, a gated arm after (orders `0`, `1`) | refuse — the else is not last |
| two conditionless arms | refuse — the earlier is not last |
| else last but the arms do not ascend | refuse |
| a `succession` among or before the arms | accept — the fall-through is not an arm |
| a non-`branch` node with any edges | ignored |

## Integration

- **`PlaybookReader.Default`** gains the checker through
  `PlaybookCheckerFactory.CreateDefault()`, after `OutwardShapeChecker`. Every
  runtime built on the default reader refuses these documents from then on.
- **The playbook round-trip property** — everything the writer produces the reader
  accepts — remains a useful guard, though only one-directional: it catches a
  checker that is too strict, never one too lax. The compiler fans a `branch` out
  in source order with `order` the arm's index, so every compiled branch has
  ascending arms with any `else` last and the checker never rejects valid compiler
  output. A direct writer assertion that each emitted branch is sorted this way
  would guard the other direction.
- **The conformance corpus** gains the new `readable/` cases, so a port that fails
  to refuse an out-of-order branch fails conformance. The README's "caught by"
  table and its "Eight of the twelve refusals" count grow with the new
  reader-only refusals, and its `baseline` paragraph is narrowed per
  [decision 5](#5-a-richer-conformance-case-ships-its-own-accepted-source).
- **`schema/playbook-0.schema.json`** gains the else cap, so a two-else document
  fails the schema as well as the reader.

## Testability

| Layer | Test |
| --- | --- |
| Unit | `BranchArmOrderCheckerTests` — an accept and a refuse for each row of the rule; each refusal starts from a well-ordered branch and breaks one thing. Nodes are built through the shared test factory. |
| Ignore | A non-`branch` node carrying edges in any order is left alone, and a `succession` sitting among the arms is accepted. |
| Property | Wired into the round-trip property: every compiled script's playbook passes the checker. |
| Cross-runtime | The new `readable/` cases — an out-of-order branch, an else not last, a repeated order, and a two-else branch. |
| Schema | CI validates every golden and every accepted conformance playbook against the schema; the goldens' else-less branches are what protect `minContains: 0`, and a two-else document is what the reader refuses. |

## Open questions

1. **Should `order` be dense?** The rule allows gaps (`0`, `2`), taking strictly
   ascending as the invariant. Requiring `0..n-1` would make the field a
   restatement of the array index; a document with gaps is unambiguous either way,
   so it is accepted. Flagged in case a future reader wants the stronger form.
2. **Does the runner sort, or trust the array?** Since the rule forces the two to
   agree, either works. Which one the runner does belongs to its own note.

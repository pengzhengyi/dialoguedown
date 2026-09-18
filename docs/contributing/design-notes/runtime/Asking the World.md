# Asking the world

> [!NOTE]
> Status: **proposed**. The pass that lets a run ask the world a question and use
> the answer, so a condition is evaluated instead of refused. It builds on the
> [runtime core](./Runtime%20Core.md), whose protocol and harness it extends, and
> on [waiting on the host](./Waiting%20on%20the%20Host.md), whose reverse-request
> shape it follows. It applies the
> [dialogue runtime architecture](./Dialogue%20Runtime%20Architecture.md), which
> owns the cross-cutting decisions this note uses, and does not restate them.

## Table of contents

- [Goal and scope](#goal-and-scope)
- [What the corpus already fixes](#what-the-corpus-already-fixes)
- [Functionality checklist](#functionality-checklist)
- [Interfaces and abstractions](#interfaces-and-abstractions)
- [Key design decisions](#key-design-decisions)
- [Error and boundary cases](#error-and-boundary-cases)
- [Integration](#integration)
- [Testability](#testability)
- [Open questions and deferred work](#open-questions-and-deferred-work)

## Goal and scope

Today a run refuses the moment it meets a condition. `Arrival` looks for one
before it looks at the node's kind, and answers with `UnansweredCondition`, whose
own remark calls it temporary. Six of the eleven node and edge kinds the format
defines can carry a condition, so that refusal stands between the runner and most
of what a writer can write.

This pass replaces it with the real thing: the run asks the driver what the world
says, waits for the answer, and uses it. Three constructs follow from that one
exchange, and they are one feature rather than three — each asks the driver about
state and reads back an answer.

| Construct | The question | What the answer decides |
| --- | --- | --- |
| A guarded line or control block | Does this key hold? | Whether the node plays at all |
| A guarded jump, option, or branch arm | Does this key hold? | Whether that way out is taken |
| A query in speech | What is this key's value? | What the line says |

In scope:

- `Resolve` and `Supply`, the second pair of reverse request and answer, and
  `AwaitingSupply`, the stage a run is at between the two;
- gathering every key one node needs into a single ask;
- evaluating a `key` condition on a node and on an edge;
- `BranchNode`, whose arms are tried in order until one holds;
- substituting a `QueryFragment` in speech with what the world said;
- retiring `UnansweredCondition`.

Out of scope, each with the pass that owns it: choices (C2b), `Describe` (C2e),
saves (C2f), and `PlaySession` with its drivers (C2g). Dynamic weights need a
number from the world and entropy to spend it on, and wait for the pass that plays
a random choice.

## What the corpus already fixes

Unusually for a first pass, the contract is not open. The published fixture
schema already defines `resolve`, `supply`, and `asked`, and four corpus cases
pin the exchanges. This note is therefore about how the runner meets a contract
that exists, not about choosing one.

| Case | What it fixes |
| --- | --- |
| `a-conditional-line` | One `resolve` naming one key, a `supply` of `false`, and then the **next** line is said — a failing condition steps over the node rather than stopping at it |
| `a-conditional-block` | The arms are tried in the order written, and a satisfied first arm means the `else` is never reached |
| `a-query-in-speech` | The supplied answer appears in the flattened speech: `Hello, Robin.` |
| `an-unavailable-option` | A failing option is offered **unavailable** rather than hidden. Needs choices too, so it still will not play after this pass |

The schema also fixes the shapes: `resolve` is a non-empty array of key strings,
and `supply` is an object keyed by those strings, whose values are whatever JSON
holds — `false` for a guard, `"Robin"` for a query.

## Functionality checklist

- [ ] A node carrying a condition asks the world about it rather than refusing.
- [ ] A node whose condition fails is stepped over, and the run carries on.
- [ ] An edge whose condition fails is not taken; one whose condition holds is.
- [ ] A branch node takes the first arm, in `order`, whose condition holds.
- [ ] A branch node with no satisfied arm and no `else` leads nowhere, and says so.
- [ ] Every key one node needs is asked for in a single `Resolve`.
- [ ] A query in speech is replaced by what the world said before the line is said.
- [ ] A key asked and left unanswered is refused, and so is a key answered that
      nobody asked about.
- [ ] An answer of the wrong kind for the question is refused.
- [ ] `UnansweredCondition` is gone, and nothing produces it.

## Interfaces and abstractions

| Type | Responsibility | Collaborators |
| --- | --- | --- |
| `Resolve(keys)` | A reverse request: the keys this node needs answered | `Request`, alongside `Perform` |
| `Supply(answers)` | The command answering it | `Command` |
| `Answer` | What the world said about one key, as a closed union | `AnswerJsonConverter` |
| `AnswerJsonConverter` | Reads and writes an answer as the bare JSON value it is | Every reader of a supply |
| `AwaitingSupply(node, keys)` | Where a run is between the ask and its answer | `Situation`, alongside `AwaitingDone` |
| `Questions` | Reads every key one node needs, in one place | `Arrival` |
| `Answers` | Holds what came back, once it matches what was asked, and reads a key as a truth or as text | `Questions`, `Evaluation` |
| `Evaluation` | Answers whether a condition holds, given what came back | `Arrival`, `NodeTraversalExtensions` |

## Key design decisions

### A1 — One ask per node, not one per condition

A node's arrival gathers every key it needs — its own condition, its outgoing
edges' conditions, and the queries in its speech — and asks for them together.

The architecture note already settles why: a per-node batch is a **snapshot**, so
evaluation within one node is a repeatable read. A menu whose options are guarded
by the same key cannot offer one and refuse another. It also means a run stops at
most once per node, which is what keeps the protocol readable.

### A2 — The runner does not remember an answer

Two lines that ask the same key produce two `Resolve`s. The runner is a total
function of state and command, and holding an answer between steps is state.

The two asks are also genuinely two questions. The world may have changed between
them, and the driver is entitled to answer differently the second time; a runner
that reused the first answer would report something the world never said. Caching
belongs to the driver, which is free to hold an answer or serve from a snapshot as
its host requires. What the protocol guarantees is read-your-own-writes, already
bought by `Perform` being answered before the run goes on.

### A3 — An answer is a closed union

`IGameWorld` already splits the world's three questions by the type of their
answers: a guard needs a truth, a weight a number, and interpolation text. The
wire agrees — `supply` carries `false` for one and `"Robin"` for another.

So `Answer` is a closed union in the manner of every other union in the format,
and its members take the same `<Qualifier><Base>` shape as `TextFragment` and
`KeyCondition` do. This pass needs two:

| Member | Wire | Used by |
| --- | --- | --- |
| `TruthAnswer(bool)` | `true` / `false` | A condition on a node or an edge |
| `TextAnswer(string)` | a JSON string | A query in speech |

A third member, for numbers, joins them when dynamic weights arrive. Adding a
member to a closed union is additive here, and a member nothing produces would be
dead code today.

Unlike the format's unions, this one needs no class of wire tags. A supplied
answer carries no discriminator, because the JSON value already is one:

```json
{ "Alice.HasKey": false, "playerName": "Robin" }
```

`AnswerJsonConverter` is where that rule lives — a boolean is a truth, a string is
words, and anything else is refused rather than guessed at, since a number could
be a weight or a count and nothing on the wire says which. Keeping it in the
package rather than in each reader is what stops a fixture, a recorded session,
and a driver on the far side of a socket from each inventing their own.

### A4 — The runner checks the answers against its own questions

A driver may answer something nobody asked, or leave something asked unanswered.
Both are refused, and the check needs no memory to make: the keys the run asked
about are carried in the situation, so the comparison is set equality between what
was asked and what came back.

That makes it a function of two collections and nothing else, which is why it
lives in `Answers` rather than inside arrival. `Questions` builds the set going
out, `Answers` checks the set coming back, and each can be tested without a
playbook in sight.

### A5 — The runner is the mechanism; the driver is the policy

A driver may answer from a live world, a cache, a recorded session, or a table of
defaults, and may have its own rules for a key nobody bound. None of that reaches
the runner, which asks a question and reads an answer.

The separation is worth stating because the architecture note's permissive default
— a script plays with no bindings at all — is easy to mistake for something the
runner does. It is not: by the time a key arrives in `Supply`, the driver has
already applied whatever policy it has. This is the same line A2 draws for
caching, seen from the other side.

### A6 — A failing condition routes; it does not refuse

`a-conditional-line` fixes this: with `Alice.HasKey` false, the next thing the run
says is the *following* line. So a node whose own condition fails is stepped over
the way an empty control node already is, and `Arrival`'s walk carries on to
whatever it leads to. The ring bound that already guards that walk covers a ring
of skipped nodes at no extra cost.

An edge's condition is a different question with a different answer: an arm whose
condition fails is simply not among the ways out. `OnwardTarget` grows a filter
rather than a new concept.

### A7 — What the run is doing lives in the situation

`AwaitingSupply(node, keys)` holds both the node the run is standing at and the
keys it asked about, exactly as the runtime core note's state diagram drew it.

Carrying the keys is what lets the runner check that the driver answered the
question it asked. Carrying the node is what lets the step that receives `Supply`
finish the arrival it started — because nothing was remembered, that step
re-reads the node and evaluates it with the answers in hand.

### A8 — A branch node is walked past, not stood at

A branch says nothing and asks the host for nothing. It exists to choose an arm.
So it belongs to the same family as the empty control node: the walk resolves its
arms, takes the first that holds, and carries on to the target without the player
ever being asked to advance past it.

The arms' order is not this pass's to decide. The reader already guarantees that a
branch's arms appear in strictly ascending `order`, that at least one is gated, and
that a conditionless `else` comes last, so the runner tries them as it finds them.

### A9 — A query is substituted before the line is said

`Said` carries speech as fragments, and `QueryFragment` carries a key. When the
answers are in hand, each query fragment becomes a `TextFragment` holding what the
world said, and the line is said with that speech.

Doing the substitution in the speech itself, rather than at the point text is
flattened, means every reader of a `Said` sees the same words — the fixture that
compares flattened text, a host that renders fragments with styles, and a log that
replays. It also leaves `SpeechText` with nothing new to know.

## Error and boundary cases

| Case | Behavior |
| --- | --- |
| `Supply` when nothing was asked | Refused as misplaced, as `Done` already is |
| `Next` while the run awaits a supply | Refused as misplaced |
| A key that was asked and not answered | `UnansweredKey`, naming the key |
| A key answered that was not asked | `UnaskedKey`, naming the key |
| An answer of the wrong kind for its use — text where a guard needs a truth | `WrongAnswerKind`, naming the key and both kinds |
| A branch whose arms all fail, with no `else` | Leads nowhere, which the existing reason already covers |
| A skipped node whose succession leads nowhere | Leads nowhere |
| A ring of nodes whose conditions all fail | The existing ring bound refuses it |
| A node with a condition **and** a query in its speech | One ask carrying both keys |

The three refusals divide one driver mistake three ways on purpose. A reason is
what a fixture compares, so a port that answers the wrong question and a port that
answers with the wrong type disagree with us for reasons a reader can tell apart.
The first two are caught by `Answers` checking the set; the third by `Answers`
reading a key as the kind its use requires.

`UnansweredCondition` is removed rather than left unused. It is a published
refusal reason, and a reason nothing can produce is a promise the corpus would
keep testing for no one.

## Integration

| Seam | Change |
| --- | --- |
| `protocol` | `Resolve` joins `Perform` as a request; `Supply` joins the commands; `Answer` and its JSON converter are new |
| `situations` | `AwaitingSupply` joins `AwaitingDone` |
| `Arrival` | Asks before it plays; steps over a node whose condition fails |
| `Questions`, `Answers`, `Evaluation` | New, and each testable without a playbook |
| `NodeTraversalExtensions` | Reads the way onward from the arms whose conditions hold |
| `Runner.Step` | One more arm: `Supply` advances from `AwaitingSupply` |
| Harness | A `ResolveMatcher`, and `supply` among the commands a session can send |
| `PlayableConformanceTests` | Three cases join the conforming list |
| `PlayableRun.IsPlayable` | Learns `BranchNode`, and its agreement test holds it to the runner |
| `PlaybookGen` | Draws conditions, branches, and queries, and its coverage test **fails until it does** |
| Runtime core note | Its state diagram's dotted C2c edge becomes a solid one |

The last two rows are the ratchet working as designed. Teaching the runner a kind
makes two lists fail by name, which is the reminder this pass is owed.

## Testability

| Level | What it covers |
| --- | --- |
| Unit — questions | Every source of a key: a node's own condition, its arms', and its speech; one node needing all three |
| Unit — answers | The set matching; a key missing; a key nobody asked about; a key read as the wrong kind |
| Unit — reading a supply | Each kind read and written back; a number, a null, and a structure all refused |
| Unit — evaluation | A condition that holds, and one that fails |
| Unit — arrival | A failing node stepped over; a failing arm not taken; a branch taking its first holding arm and its `else` |
| Unit — speech | A query substituted; a line with text and a query together; a query whose answer is empty |
| Unit — the protocol | `Supply` where nothing was asked; `Next` while awaiting a supply |
| Property | The walk property, widened: a run still only ever stands where the playbook has a node |
| Conformance | `a-conditional-line`, `a-conditional-block`, and `a-query-in-speech` conform |

Three are worth naming because they are easy to leave out. A node with a condition
**and** a query must produce **one** `Resolve` carrying both keys, which is the
half of A1 a single-key fixture cannot show. A branch whose arms all fail with no
`else` must say it leads nowhere rather than hanging. And a ring of nodes whose
conditions all fail must be refused by the existing bound — the guard was written
for empty control nodes, and this pass gives it a second kind of walker.

## Open questions and deferred work

- **`an-unavailable-option` needs choices as well.** It is the one corpus case
  this pass touches without finishing; it stays named as not yet runnable until
  C2b lands.
- **`IGameSystem` still has its placeholder name.** The architecture note says the
  rename to `IGameWorld` lands with C2. This pass is the first that has a world to
  name, so it is the natural place, but it is a wider change than the protocol.

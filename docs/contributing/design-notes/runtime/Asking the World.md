# Asking the world

> [!NOTE]
> Status: **implemented**. The pass that lets a run ask the world a question and
> use the answer, so a condition is evaluated instead of refused. A node's own
> condition, the queries standing in its speech, a jump's condition, and a block
> condition's arms are asked and answered. An option's condition arrives with
> choices.
>
> It builds on the [runtime core](./Runtime%20Core.md), whose protocol and
> harness it extends, and on
> [waiting on the host](./Waiting%20on%20the%20Host.md), whose reverse-request
> shape it follows. It applies the
> [dialogue runtime architecture](./Dialogue%20Runtime%20Architecture.md), which
> owns the cross-cutting decisions this note uses, and does not restate them.

## Table of contents

- [Goal and scope](#goal-and-scope)
- [What the corpus fixes](#what-the-corpus-fixes)
- [Functionality checklist](#functionality-checklist)
- [Interfaces and abstractions](#interfaces-and-abstractions)
- [Key design decisions](#key-design-decisions)
- [Error and boundary cases](#error-and-boundary-cases)
- [Integration](#integration)
- [Testability](#testability)
- [Open questions and deferred work](#open-questions-and-deferred-work)

## Goal and scope

Six of the eleven node and edge kinds the format defines can carry a condition,
so a runner that cannot read one cannot play most of what a writer can write.

This pass lets a run ask the driver what the world says, wait for the answer, and
use it. Three constructs follow from that one exchange, and they are one feature
rather than three — each asks the driver about state and reads back an answer.

| Construct | The question | What the answer decides |
| --- | --- | --- |
| A guarded line or control block | Does this key hold? | Whether the node plays at all |
| A guarded jump or branch arm, and an option once choices play | Does this key hold? | Whether that way out is taken |
| A query in speech | What is this key's value? | What the line says |

In scope:

- `Resolve` and `Supply`, the second pair of reverse request and answer, and
  `AwaitingSupply`, the stage a run is at between the two;
- gathering the keys a node needs into one ask per moment: one before the node
  plays, and one before the run leaves it;
- evaluating a `key` condition on a node, on a jump, and on a block condition's
  arm;
- `BranchNode`, whose arms are tried in order until one holds;
- substituting a `QueryFragment` in speech with what the world said;
- retiring `UnansweredCondition`.

Out of scope, each with the pass that owns it: choices, and an option's
condition with them (C2b); `Describe` (C2e); saves (C2f); and `PlaySession` with
its drivers, and the world a driver answers from (C2g). Dynamic weights need a
number from the world and entropy to spend it on, and wait for the pass that plays
a random choice.

## What the corpus fixes

The contract is not this note's to choose. The published fixture schema defines
`resolve`, `supply`, and `asked`, and five corpus cases pin the exchanges, so this
note is about how the runner meets that contract.

| Case | What it fixes |
| --- | --- |
| `a-conditional-line` | One `resolve` naming one key, a `supply` of `false`, and then the **next** line is said — a failing condition steps over the node rather than stopping at it |
| `a-conditional-jump` | A jump with nothing to say still stops to ask: one `resolve` before the run walks past it, and a `supply` of `true` takes the jump, so the line after it is never said |
| `a-conditional-block` | The arms are tried in the order written, and a satisfied first arm means the `else` is never reached |
| `a-query-in-speech` | The supplied answer appears in the flattened speech: `Hello, Robin.` |
| `an-unavailable-option` | A failing option is offered **unavailable** rather than hidden. Needs choices too, so it does not play yet |

The schema also fixes the shapes: `resolve` is a non-empty array of key strings,
and `supply` is an object keyed by those strings, whose values are whatever JSON
holds — `false` for a guard, `"Robin"` for a query.

## Functionality checklist

- [x] A node carrying a condition asks the world about it rather than refusing.
- [x] A node whose condition fails is stepped over, and the run carries on by its
      succession, never by its own jump.
- [x] A jump or a block condition's arm whose condition fails is not taken; one
      whose condition holds is.
- [ ] An option whose condition fails is offered unavailable. Deferred to choices
      (C2b).
- [x] A branch node takes the first arm, in `order`, whose condition holds.
- [x] A branch node with no satisfied arm and no `else` falls through to the
      succession beneath the block, so the block is skipped; one with no
      succession either leads nowhere, and says so.
- [x] Every key a moment needs is asked for in a single `Resolve`: one on the way
      in, and one on the way out.
- [x] A query in speech is replaced by what the world said before the line is said.
- [x] A key asked and left unanswered is refused, and so is a key answered that
      nobody asked about.
- [x] An answer of the wrong kind for the question is refused.
- [x] A key one node needs as a truth and as words both is refused, because a
      single answer can only be one of those.
- [x] `UnansweredCondition` is gone, and nothing produces it.

## Interfaces and abstractions

| Type | Responsibility | Collaborators |
| --- | --- | --- |
| `Resolve(keys)` | A reverse request: the keys one moment at a node needs answered | `Request`, alongside `Perform` |
| `Supply(answers)` | The command answering it | `Command` |
| `Answer` | What the world said about one key, as a closed union | `AnswerJsonConverter` |
| `AnswerJsonConverter` | Reads and writes an answer as the bare JSON value it is | Every reader of a supply |
| `AwaitingSupply(node, keys, moment)` | Where a run is between the ask and its answer | `Situation`, alongside `AwaitingDone` |
| `Moment` | Whether the run asked before the node plays (`ToPlay`) or before the run leaves it (`ToLeave`) | `AwaitingSupply`, `Runner` |
| `NodeQuestionExtensions` | Reads a node's keys, one reader per moment and per kind of answer | `NodeQuestions` |
| `Questions` | Turns a moment's two sets of keys into what goes out and what comes back is held to | `NodeQuestions` |
| `NodeQuestions` | One moment's keys, read together so the two kinds cannot drift apart | `Arrival`, `Departure` |
| `AnswerCheck` | Holds what came back to what was asked, and names every way they disagree | `Arrival`, `Departure` |
| `SupplyExtensions` | Reads a key from a supply as a truth or as words | `ConditionEvaluationExtensions`, `Arrival` |
| `ConditionEvaluationExtensions` | Answers whether a condition holds, and whether a guarded node or way out is allowed | `Arrival`, `NodeTraversalExtensions` |
| `Arrival` | Arriving at a node: asks what playing it needs, then plays it or walks past it | `Runner`, `Departure` |
| `Departure` | Leaving a node: asks which way out when its ways out are guarded, then arrives where it leads | `Runner`, `Arrival` |
| `NodeTraversalExtensions` | Where a node leads: the first jump or arm taken, then the succession | `Arrival`, `Departure` |
| `StepResults` | The ask and the refusal both halves produce | `Arrival`, `Departure` |
| `SpeechTemplate` | Names the keys standing in speech, and fills them with words (in the playbook package) | `NodeQuestionExtensions`, `Arrival` |

## Key design decisions

### A1 — One ask per moment, and a node has two of them

A run reads the world twice at a node, and each reading gathers every key that
moment needs and asks for them together.

**Arriving** asks what decides whether the node plays and what it says: the
node's own condition, answered with a truth, and the queries standing in its
speech, answered with words. **Leaving** asks what decides which way out is
taken: the conditions on its jump or on a block condition's arms.

The two are kept apart because the node changes the world between them. A node
that performs an effect has that effect carried out after it is played and before
the run reads on, so an arm guarded by a key the effect touches must be judged
against the world the effect left behind. Asking on the way in would read the
world as it was before, and the run would take a way out the writer did not mean.
This is the same read-your-own-writes guarantee `Perform` already buys, applied
to the keys that decide succession.

Within one moment the batch is a **snapshot**, so evaluation is a repeatable
read: a menu whose options are guarded by the same key cannot offer one and
refuse another. A run therefore stops at most once on the way in and at most once
on the way out, which is what keeps the protocol readable.

A moment asks about every key it might need, not only the ones that end up
deciding. A block's `elseif` is asked about even when its `if` holds and the
`elseif` is never reached. That is sound because a query is a pure read
([D6](./Dialogue%20Runtime%20Architecture.md#d6--queries-are-pure-reads-effects-change-the-world)):
reading a key changes nothing, so reading one more costs only the read, and the
driver may answer the keys in any order, one at a time or all at once.

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

The world seam the architecture note designs, `IGameWorld`, splits the world's
three questions by the type of their answers: a guard needs a truth, a weight a
number, and interpolation text. The wire agrees — `supply` carries `false` for one and `"Robin"` for another.

So `Answer` is a closed union in the manner of every other union in the format,
and its members take the same `<Qualifier><Base>` shape as `TextFragment` and
`KeyCondition` do. This pass needs two:

| Member | Wire | Used by |
| --- | --- | --- |
| `BooleanAnswer(bool Holds)` | `true` / `false` | A condition on a node or on a way out |
| `TextAnswer(string Text)` | a JSON string | A query in speech |

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
lives in `AnswerCheck` rather than inside arrival or departure. `NodeQuestions` builds the set
going out, `AnswerCheck` holds the set coming back to it, and each can be tested
without a playbook in sight.

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
says is the *following* line. So a node whose own condition fails is stepped over,
and the run arrives at whatever its succession leads to. It takes the succession
alone: a jump belongs to the node that carries it, so a line the world withheld
does not send the reader through the door it opens. A node the world allows is
entered as if it had needed no answers: played with the answers in it, or walked
past when it hands the host nothing.

A guarded node stops the walk to ask. So a loop that comes back round to one asks
about it again, and the world may answer differently the next time; the ring
bound only ever meets nodes that nothing guards.

An edge's condition is a different question with a different answer: a way out
whose condition fails is simply not among the ways out. Where a node leads is one
rule, read with the answers or without them: the first jump or arm taken, then
the succession. With answers, a way out is taken when the world allows it. Without
them, only a way out that nothing guards is taken, because nobody asked the world
about the rest.

### A7 — What the run is doing lives in the situation

`AwaitingSupply(node, keys, moment)` holds the node the run is standing at, the
keys it asked about, and the moment it asked at.

Carrying the keys is what lets the runner check that the driver answered the
question it asked. Carrying the node is what lets the step that receives `Supply`
finish what it started — because nothing was remembered, that step re-reads the
node and evaluates it with the answers in hand. Carrying the moment says what it
started: `Runner` hands an answer given before the node plays to `Arrival`, and
one given before the run leaves it to `Departure`. The keys alone cannot say which,
because one key can be asked at both moments of one node.

### A8 — A branch node is walked past, not stood at

A branch says nothing and asks the host for nothing. It exists to choose an arm.
So it belongs to the same family as the empty control node: the walk resolves its
arms, takes the first that holds, and carries on to the target without the player
ever being asked to advance past it.

The arms' order is not this pass's to decide. The reader already guarantees that a
branch's arms appear in strictly ascending `order`, that at least one is gated, and
that a conditionless `else` comes last, so the runner tries them as it finds them.

A branch has nothing to ask on the way in, so all of its reading happens on the way
out: one ask carrying every arm's key, then the first arm the answers allow. When
none holds and there is no `else`, the run falls through to the succession the
compiler writes beneath the block, so the block is skipped, as the guide describes.

A jump on its own line is walked past the same way, and its condition is asked
on the way out too. Both ask inside the walk rather than by leaving through
`Departure`, so the ring bound still counts every node the walk passes.

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
| A branch whose arms all fail, with no `else` | Falls through to the succession beneath the block, so the block is skipped. Leads nowhere only when there is no succession either, which no script compiles to |
| A skipped node whose succession leads nowhere | Leads nowhere |
| A guarded node with nothing to say or perform, once the world allows it | Walked past, as an unguarded one is. No script compiles to this, but a reader accepts it |
| A loop the world keeps withholding every guarded node of | Each guarded node is asked about again as the walk comes back to it; the ring bound is never reached |
| A node with a condition **and** a query in its speech | One ask carrying both keys |
| A node naming one key as its condition **and** as a query | `KeyNeededBothWays`, naming the key, refused before anything is asked, and again when a run restored straight into the wait is answered |

The three refusals divide one driver mistake three ways on purpose. A reason is
what a fixture compares, so a port that answers the wrong question and a port that
answers with the wrong type disagree with us for reasons a reader can tell apart.
The first two are caught by `AnswerCheck` holding the set to what was asked; the
third by the kind each key was asked under. Each leaves the run waiting where it
asked, so a driver that misread the request can answer it again rather than lose
the conversation.

A key a node needs as a truth and as words both is a fourth reason, and not a
driver mistake at all: the playbook asks one key two ways, and a single answer
can only be one of those. It is refused on the way in, before a request goes out
that nothing could read back.

`UnansweredCondition` is removed rather than left unused. It is a published
refusal reason, and a reason nothing can produce is a promise the corpus would
keep testing for no one.

## Integration

| Seam | Change |
| --- | --- |
| `protocol` | `Resolve` joins `Perform` as a request; `Supply` joins the commands; `Answer` and its JSON converter are new |
| `situations` | `AwaitingSupply` joins `AwaitingDone`, carrying the `Moment` it asked at |
| `Arrival` | Asks what playing a node needs; steps over a node the world withholds; walks past a branch and a jump on its own line, asking about their ways out inside the walk |
| `Departure`, `StepResults` | New: leaving a node, asking first when its ways out are guarded; and the ask and the refusal it shares with `Arrival` |
| `NodeQuestions`, `Questions`, `AnswerCheck`, `ConditionEvaluationExtensions` | New, and each testable without a playbook |
| `NodeTraversalExtensions` | One rule for where a node leads: the first jump or arm taken, then the succession |
| `Runner.Step` | `Next` and `Done` leave through `Departure`; `Supply` goes to `Arrival` or `Departure` by the moment the run asked at |
| Harness | A `ResolveMatcher`, and a `SupplyReader` so `supply` is among the commands a session can send |
| `PlayableConformanceTests` | `a-conditional-line`, `a-conditional-jump`, `a-conditional-block`, and `a-query-in-speech` join the conforming list |
| `PlayableRun.IsPlayable` | Learns `BranchNode`, and its agreement test holds it to the runner |
| `PlaybookGen` | Draws guards, jumps the world must allow, block conditions, and queries, and its coverage test **fails until it does**; a world drawn beside each playbook answers every question a walk meets |
| Runtime core note | Its state diagram shows `AwaitingSupply` as a stage a run reaches and leaves |

The `PlayableRun.IsPlayable` and `PlaybookGen` rows are the ratchet working as
designed. Teaching the runner a kind makes two lists fail by name, which is the
reminder a pass is owed.

## Testability

| Level | What it covers |
| --- | --- |
| Unit — questions | Every source of a key: a node's own condition, its ways out, and its speech; one node needing all three |
| Unit — answers | The set matching; a key missing; a key nobody asked about; a key read as the wrong kind |
| Unit — reading a supply | Each kind read and written back; a number, a null, and a structure all refused |
| Unit — evaluation | A condition that holds, and one that fails |
| Unit — arrival and departure | A withheld node stepped over by its succession, not its jump; an allowed node with nothing to hand the host walked past; a loop asked about again; a withheld jump not taken; a block asking about every arm at once |
| Unit — ways out | The first jump or arm taken, then the `else`, then the succession, read with answers and without |
| Unit — speech | A query substituted; a line with text and a query together; a query whose answer is empty |
| Unit — the protocol | `Supply` where nothing was asked; `Next` while awaiting a supply |
| Property | The walk property, widened: the generator draws guards, jumps the world must allow, block conditions, and queries, and a world drawn beside each playbook answers every wait. A run still only ever stands where the playbook has a node, and never turns an answer away |
| Conformance | `a-conditional-line`, `a-conditional-jump`, `a-conditional-block`, and `a-query-in-speech` conform |

Three are worth naming because they are easy to leave out. A node with a condition
**and** a query must produce **one** `Resolve` carrying both keys, which is the
half of A1 a single-key fixture cannot show. A branch whose arms all fail with no
`else` must skip the block rather than hang. And a loop that comes back to a
guarded node must ask about it again: the ring bound was written for nodes that
nothing guards, and a guarded node stops the walk before the bound is reached.

## Open questions and deferred work

- **An option's condition arrives with choices.** `an-unavailable-option` is the
  one corpus case this pass touches without finishing; it stays named as not yet
  runnable until C2b lands.
- **`IGameSystem` still has its placeholder name.** Nothing in this pass reads a
  world directly: the runner asks the driver. So the rename to `IGameWorld` waits
  for C2g, where a driver answers `Resolve` from a world.
- **A key used both ways is caught only at play time.** The compiler does not yet
  reject a script that uses one key as a guard and as a query on the same node, so
  `KeyNeededBothWays` is what catches it.

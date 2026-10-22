# Asking the World

> [!NOTE]
> Status: **implemented**. How a run asks the world a question and uses the
> answer, so a condition decides where the run goes. A node's own condition, the
> queries standing in its speech, a jump's condition, and a block condition's
> branches are asked and answered, and so are a menu's options' conditions and its
> labels' queries (see [Offering a Choice](./Offering%20a%20Choice.md)) and a random
> choice's conditions and key weights (see
> [Drawing a Random Option](./Drawing%20a%20Random%20Option.md)).
>
> It extends the [Runner](./Runner.md)'s protocol and harness, and follows the
> shape of its wait on the host. It applies the
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

A run asks the driver what the world says, waits for the answer, and uses it. Three constructs follow from that one exchange, and they are one feature
rather than three — each asks the driver about state and reads back an answer.

| Construct | The question | What the answer decides |
| --- | --- | --- |
| A guarded line or control block | Does this key hold? | Whether the node plays at all |
| A guarded jump, branch, or option | Does this key hold? | Whether that way out is taken |
| A query in speech | What is this key's value? | What the line says |

In scope:

- `Resolve` and `Supply`, the second pair of reverse request and answer, with
  each key in a request named under the kind of answer it needs, and
  `AwaitingSupply`, the stage a run is at between the two;
- gathering the keys a node needs into one ask per moment: one before the node
  plays, and one before the run leaves it;
- evaluating a `key` condition on a node, on a jump, and on a block condition's
  branch;
- `BranchNode`, whose branches are tried in order until one holds;
- substituting a `QueryFragment` in speech with what the world said.

Out of scope: a menu, and an option's condition with it, which
[Offering a Choice](./Offering%20a%20Choice.md) owns; `Describe`; saves; and
`PlaySession` with its drivers, and the world a driver answers from. Key weights
need a number from the world and a draw to spend it on, and belong to
[Drawing a Random Option](./Drawing%20a%20Random%20Option.md).

## What the corpus fixes

The published fixture schema defines `resolve`, `supply`, and `offer`, and the
corpus cases below pin the exchanges, so most of this note is about how the
runner meets that contract.

| Case | What it fixes |
| --- | --- |
| `a-conditional-line` | One `resolve` naming one key, a `supply` of `false`, and then the **next** line is said — a failing condition steps over the node rather than stopping at it |
| `a-conditional-jump` | A jump with nothing to say still stops to ask: one `resolve` before the run walks past it, and a `supply` of `true` takes the jump, so the line after it is never said |
| `a-conditional-block` | The branches are tried in the order written, and a satisfied first branch means the `else` is never reached |
| `a-query-in-speech` | The supplied answer appears in the flattened speech: `Hello, Robin.` |
| `an-unavailable-option` | A failing option is offered **unavailable** rather than hidden |
| `a-menu-asking-truths-and-words` | One request asks for two kinds of answer: the options' conditions under `boolean` and the labels' queries under `text` |

The schema also fixes the shapes. `resolve` is an object that groups the keys by
the kind of answer each needs — `boolean`, `text`, and `number`. Each group is a
non-empty array of distinct key strings, left out when the moment needs none of
that kind, and a request always has at least one.
`supply` is an object keyed by those strings, whose values are whatever JSON holds:
`true` for a key under `boolean`, `"Robin"` for one under `text`.

```json
[
  { "expect": { "resolve": { "boolean": ["Alice.HasKey"], "text": ["playerName"] } } },
  { "send": { "supply": { "Alice.HasKey": true, "playerName": "Robin" } } }
]
```

## Functionality checklist

- [x] A node carrying a condition asks the world about it rather than refusing.
- [x] A node whose condition fails is stepped over, and the run carries on by its
      succession, never by its own jump.
- [x] A jump or a block condition's branch whose condition fails is not taken; one
      whose condition holds is.
- [x] An option whose condition fails is offered unavailable, as
      [Offering a Choice](./Offering%20a%20Choice.md) designs.
- [x] A branch node takes the first branch, in the order written, whose condition
      holds.
- [x] A branch node with no satisfied branch and no `else` falls through to the
      succession beneath the block, so the block is skipped; one with no
      succession either leads nowhere, and says so.
- [x] Every key a moment needs is asked for in a single `Resolve`: one on the way
      in, one at each stop inside a line, and one on the way out.
- [ ] A request names each key under the kind of answer it needs — a truth,
      words, or a number — so a driver knows what to answer with.
- [x] A query in speech is replaced by what the world said before the line is said.
- [x] A key asked and left unanswered is refused, and so is a key answered that
      nobody asked about.
- [x] An answer of the wrong kind for the question is refused.
- [x] A key one node needs as two kinds of answer is refused, because a single
      answer can only be one kind.

## Interfaces and abstractions

| Type | Responsibility | Collaborators |
| --- | --- | --- |
| `Resolve(booleanKeys, textKeys, numberKeys)` | A reverse request: the keys one moment at a node needs answered, grouped by the kind of answer each needs | `Request`, alongside `Perform` |
| `Supply(answers)` | The command answering it | `Command` |
| `Answer` | What the world said about one key, as a closed union | `AnswerJsonConverter` |
| `AnswerJsonConverter` | Reads and writes an answer as the bare JSON value it is | Every reader of a supply |
| `AwaitingSupply(node, keys, moment)` | Where a run is between the ask and its answer | `Situation`, alongside `AwaitingDone` |
| `Moment` | Whether the run asked before the node plays (`ToPlay`) or before the run leaves it (`ToLeave`) | `AwaitingSupply`, `Runner` |
| `NodeQuestionExtensions` | Reads a node's keys, one reader per moment and per kind of answer | `NodeQuestions` |
| `Questions` | Turns a moment's three sets of keys, truths, words, and numbers, into what goes out and what comes back is held to | `NodeQuestions` |
| `NodeQuestions` | One moment's keys, read together so the kinds cannot drift apart | `Arrival`, `Departure` |
| `AnswerCheck` | Holds what came back to what was asked, and names every way they disagree | `Arrival`, `Departure` |
| `SupplyExtensions` | Reads a key from a supply as a truth, as words, or as a number | `ConditionEvaluationExtensions`, `Arrival`, `AnsweredKeyWeights` |
| `ConditionEvaluationExtensions` | Answers whether a condition holds, and whether a guarded node or way out is allowed | `Arrival`, `NodeTraversalExtensions` |
| `Arrival` | Arriving at a node: asks what playing it needs, then plays it or walks past it | `Runner`, `Departure` |
| `Departure` | Leaving a node: asks which way out when its ways out are guarded, then arrives where it leads | `Runner`, `Arrival` |
| `NodeTraversalExtensions` | Where a node leads: the first jump or branch taken, then the succession | `Arrival`, `Departure` |
| `StepResults` | The ask and the refusal both halves produce | `Arrival`, `Departure` |
| `SpeechTemplate` | Names the keys standing in speech, and fills them with words (in the playbook package) | `NodeQuestionExtensions`, `Arrival` |

## Key design decisions

### A1 — One ask per moment

A run reads the world twice at a node, and each reading gathers every key that
moment needs and asks for them together. A line with a query written after a
command has one more moment at each [stop](./Speaking%20a%20Line.md#s8--the-world-is-asked-once-per-stop) inside it.

**Arriving** asks what decides whether the node plays and what it says: the
node's own condition, answered with a truth, and the queries standing in its
speech up to its first stop, answered with words. **Leaving** asks what decides which way out is
taken: the conditions on its jump or on a block condition's branches. A menu asks its
options' conditions and its labels' queries on arriving, and nothing on leaving,
because its options are taken by choosing. A random choice asks its random options'
conditions and key weights on arriving, a key weight answered with a number, and
nothing on leaving
([R3](./Drawing%20a%20Random%20Option.md#r3--what-decides-the-draw-is-read-once-on-arrival)).

A request names each key once, under the kind of answer it needs
([A3](#a3--a-key-is-asked-and-answered-as-one-of-three-kinds)), and lists each
kind's keys in the order the node writes them, so every runtime reading the same
playbook sends the same request.

The two are kept apart because the node changes the world between them. A node
that performs an effect has the host perform it after it is played and before
the run reads on, so a jump or branch guarded by a key the effect touches must be
judged against the world the effect left behind. Asking on the way in would read the
world as it was before, and the run would take a way out the writer did not mean.
This is the same read-your-own-writes guarantee `Perform` already buys, applied
to the keys that decide succession.

Within one moment the batch is a **snapshot**, so evaluation is a repeatable
read: a menu whose options are guarded by the same key cannot offer one and
refuse another. A run therefore stops at most once on the way in, once at each stop
inside a line, and once on the way out, which is what keeps the protocol readable.

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

### A3 — A key is asked and answered as one of three kinds

A node asks the world three kinds of question: a guard needs a truth, a query in
speech needs words, and a key weight needs a number. The request says which kind
each key needs, and the answer is one of the three. The wire agrees — `supply`
carries `false` for one, `"Robin"` for another, and `20` for a third.

So `Answer` is a closed union in the manner of every other union in the format,
and its members take the same `<Qualifier><Base>` shape as `TextFragment` and
`KeyCondition` do:

| Member | Wire | Used by |
| --- | --- | --- |
| `BooleanAnswer(bool Holds)` | `true` / `false` | A condition on a node, on a way out, or on a random option |
| `TextAnswer(string Text)` | a JSON string | A query in speech |
| `NumberAnswer(double Value)` | a JSON number | A key weight on a random option |

Each member answers one kind of question a node asks. Adding a member to a closed
union is additive here, and a member nothing asks for would be dead code.

Unlike the format's unions, this one needs no class of wire tags. A supplied
answer carries no discriminator, because the JSON value already is one:

```json
{ "Alice.HasKey": false, "playerName": "Robin", "Guard.Mood": 20 }
```

`AnswerJsonConverter` is where that rule lives — a boolean is a truth, a string is
words, a number is a number, and anything else is refused rather than guessed at. A
number too large for a double, such as `1e400`, reads as infinity, so the run can
refuse it as an invalid weight. Keeping the rule in the package rather than in each
reader is what stops a fixture, a recorded session, and a driver on the far side of
a socket from each inventing their own.

A request groups its keys under the same three kinds, each group named for the
member that answers it:

```json
{ "boolean": ["Guard.IsAngry"], "number": ["Guard.Mood"] }
```

`Resolve` holds the groups as `BooleanKeys`, `TextKeys`, and `NumberKeys`, and its
`Keys` lists every key — the `boolean` group, then `text`, then `number` — for a
driver that looks a key up without needing its kind. A key is in one group only, because a key one node needs as two
kinds is refused before anything is asked (see
[Error and boundary cases](#error-and-boundary-cases)); the runner keeps that rule,
and the fixture schema cannot state it. The groups are members of
a JSON object, so their order means nothing; the order within each group is the
one A1 sets.

The runner knows each key's kind when it asks, because it found the key by reading
the node. Saying so in the request spares every driver from reading the node again:

- A host that asks a person, such as a terminal prompt during a test play, offers
  yes or no for a truth, a line of text for words, and a number field for a number.
- A host that answers from its own store reads each value as the type the request
  names.

The kind belongs to the request, not to the key. One key can be words in one place
and a truth in another, even at the two moments of one node: a line can say
`` `"Hero.Title"` `` and leave by a jump guarded by `Hero.Title`. So a driver
takes the kind from each request rather than remembering it by key.

| Alternative | Why not |
| --- | --- |
| Each driver works out the kinds from the playbook | Every driver, in every language, would repeat the runner's reading of a node: its guard, the queries up to the next stop, a random choice's key weights. The corpus checks what a runner sends, not how a driver reads a playbook, so nothing would catch a driver that got it wrong |
| The world answers untyped, and the runner converts | How to convert, such as whether `"yes"` is a truth or `"20"` a number, would become part of the protocol, and a mistyped answer would become a value instead of a refusal |

### A4 — The runner checks the answers against its own questions

A driver may answer something nobody asked, or leave something asked unanswered.
Both are refused, and the check needs no memory to make: the step that receives
the answers reads the node again at the moment it asked, which gives back the keys
and kinds the request named, so the comparison is between what was asked and what
came back.

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
about it again, and the world may answer differently the next time; the loop
bound only ever meets nodes that nothing guards.

An edge's condition is a different question with a different answer: a way out
whose condition fails is simply not among the ways out. Where a node leads is one
rule, read with the answers or without them: the first jump or branch taken, then
the succession. With answers, a way out is taken when the world allows it. Without
them, only a way out that nothing guards is taken, because nobody asked the world
about the rest.

### A7 — What the run is doing lives in the situation

`AwaitingSupply(node, keys, moment)` holds the node the run is standing at, the
keys it asked about, and the moment it asked at.

Carrying the node and the moment is what lets the step that receives `Supply`
finish what it started. Because nothing was remembered, that step reads the node
again at that moment: it learns what was asked, keys and kinds, holds the answers
to it ([A4](#a4--the-runner-checks-the-answers-against-its-own-questions)), and
evaluates the node with the answers in hand. The moment says which half to finish:
`Runner` hands an answer given before the node plays to `Arrival`, and one given
before the run leaves it to `Departure`. The keys alone cannot say which, because
one key can be asked at both moments of one node.

The keys record which request is open, for anyone reading the state; the runner
does not read them back. Whether a wait keeps its whole request, kinds included, is
left to saves (see [deferred work](#open-questions-and-deferred-work)).

### A8 — A branch node is walked past, not stood at

A branch says nothing and asks the host for nothing. It exists to choose a branch.
So it belongs to the same family as the empty control node: the walk resolves its
branches, takes the first that holds, and carries on to the target without the player
ever being asked to advance past it.

The branches' order is not the runner's to decide. A branch node's branches are
tried in the order they appear in `out`, and the reader already guarantees that at
least one is gated and that a conditionless `else` comes last, so the runner tries
them as it finds them.

A branch has nothing to ask on the way in, so all of its reading happens on the way
out: one ask carrying every branch's key, then the first branch the answers allow. When
none holds and there is no `else`, the run falls through to the succession the
compiler writes beneath the block, so the block is skipped, as the guide describes.

A jump on its own line is walked past the same way, and its condition is asked
on the way out too. Both ask inside the walk rather than by leaving through
`Departure`, so the loop bound still counts every node the walk passes.

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
| A branch node whose branches all fail, with no `else` | Falls through to the succession beneath the block, so the block is skipped. Leads nowhere only when there is no succession either, which no script compiles to |
| A skipped node whose succession leads nowhere | Leads nowhere |
| A guarded node with nothing to say or perform, once the world allows it | Walked past, as an unguarded one is. No script compiles to this, but a reader accepts it |
| A loop the world keeps withholding every guarded node of | Each guarded node is asked about again as the walk comes back to it; the loop bound is never reached |
| A node with a condition **and** a query in its speech | One ask carrying both keys |
| A node naming one key as its condition **and** as a query | `KeyNeededBothWays`, naming the key, refused before anything is asked, and again when a run restored straight into the wait is answered |

The three refusals divide one driver mistake three ways on purpose. A reason is
what a fixture compares, so a port that answers the wrong question and a port that
answers with the wrong type disagree with us for reasons a reader can tell apart.
The first two are caught by `AnswerCheck` holding the set to what was asked; the
third by the kind the request named for each key. Each leaves the run waiting where it
asked, so a driver that misread the request can answer it again rather than lose
the conversation.

A key a node needs as two kinds of answer, such as a guard and a query, is a fourth
reason, and not a driver mistake at all: the playbook asks one key two ways, and a
single answer can only be one kind. It is refused on the way in, before a request goes out
that nothing could read back. The compiler rejects a script that does this as
[`DLG2018`](../../../guide/error-codes.md#dlg2018), so only a playbook written or edited by
hand reaches the refusal.

## Integration

| Part | What it holds |
| --- | --- |
| `protocol` | `Resolve`, a request beside `Perform`, naming each key under the kind of answer it needs; `Supply`, a command; `Answer` and its JSON converter |
| Fixture schema | `resolve`, with its keys grouped under `boolean`, `text`, and `number` |
| `situations` | `AwaitingSupply`, beside `AwaitingDone`, carrying the `Moment` it asked at |
| `Arrival` | Asks what playing a node needs; steps over a node the world withholds; walks past a branch and a jump on its own line, asking about their ways out inside the walk |
| `Departure`, `StepResults` | Leaving a node, asking first when its ways out are guarded; and the ask and the refusal it shares with `Arrival` |
| `NodeQuestions`, `Questions`, `AnswerCheck`, `ConditionEvaluationExtensions` | Each testable without a playbook |
| `NodeTraversalExtensions` | One rule for where a node leads: the first jump or branch taken, then the succession |
| `Runner.Step` | `Next` and `Done` leave through `Departure`; `Supply` goes to `Arrival` or `Departure` by the moment the run asked at |
| Harness | A `ResolveMatcher`, which holds each group's keys to the order written, and a `SupplyReader` so `supply` is among the commands a session can send |
| `PlayableConformanceTests` | Holds `a-conditional-line`, `a-conditional-jump`, `a-conditional-block`, and `a-query-in-speech` to their whole conversations, as it holds every case |
| `Playability.CanPlay` | Counts `BranchNode` as playable, and its agreement test holds it to the runner |
| `PlaybookGen` | Draws guards, jumps the world must allow, block conditions, and queries, and its coverage test fails while it draws none of a kind; a world drawn beside each playbook answers every question a walk meets |
| Runtime core note | Its state diagram shows `AwaitingSupply` as a stage a run reaches and leaves |

The `Playability.CanPlay` and `PlaybookGen` rows keep the harness in step with the
runner. A node kind the runner plays that either one leaves out makes a test fail
by name.

## Testability

| Level | What it covers |
| --- | --- |
| Unit — questions | Every source of a key: a node's own condition, its ways out, and its speech; one node needing all three |
| Unit — the request | Each key under the kind its use needs, each group in the order written, a key written twice in one group named once, a kind nothing needs left empty, and `Keys` listing every key |
| Unit — answers | The set matching; a key missing; a key nobody asked about; a key read as the wrong kind |
| Unit — reading a supply | Each kind read and written back; a number past what a double holds read as infinity, and refused when written back; anything else refused |
| Unit — evaluation | A condition that holds, and one that fails |
| Unit — arrival and departure | A withheld node stepped over by its succession, not its jump; an allowed node with nothing to hand the host walked past; a loop asked about again; a withheld jump not taken; a block asking about every branch at once |
| Unit — ways out | The first jump or branch taken, then the `else`, then the succession, read with answers and without |
| Unit — speech | A query substituted; a line with text and a query together; a query whose answer is empty |
| Unit — the protocol | `Supply` where nothing was asked; `Next` while awaiting a supply |
| Property | The walk property: the generator produces guards, jumps the world must allow, block conditions, and queries, and a world sampled beside each playbook answers every wait. A run still only ever stands where the playbook has a node, and never turns an answer away |
| Conformance | `a-conditional-line`, `a-conditional-jump`, `a-conditional-block`, and `a-query-in-speech` conform |

Three are worth naming because they are easy to leave out. A node with a condition
**and** a query must produce **one** `Resolve` carrying both keys, which is the
half of A1 a single-key fixture cannot show. A branch node whose branches all fail
with no `else` must skip the block rather than hang. And a loop that comes back to a
guarded node must ask about it again: the loop bound was written for nodes that
nothing guards, and a guarded node stops the walk before the bound is reached.

## Open questions and deferred work

- **What a wait keeps.** `AwaitingSupply` keeps the keys it asked about but not
  their kinds, and the runner reads the node again rather than the keys. A run
  restored straight into the wait has to send its request again, kinds included,
  so saves decide whether the wait keeps the whole request or the request is read
  from the node and moment.
- **No world interface exists yet.** Nothing in the runner reads a world directly:
  it asks the driver. So the world interface waits for the drivers that answer
  `Resolve` from a world.

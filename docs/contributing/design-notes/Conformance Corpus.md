# Conformance corpus

> [!NOTE]
> Status: **proposed** — not yet implemented. This note designs the fixtures that
> keep more than one runtime honest, and the format they are written in. It
> implements the conformance half of the
> [Dialogue runtime architecture](./Dialogue%20Runtime%20Architecture.md), which
> owns the cross-cutting decisions this note applies.

## Table of contents

- [Goal and scope](#goal-and-scope)
- [Functionality checklist](#functionality-checklist)
- [Where the corpus lives](#where-the-corpus-lives)
- [The fixture format](#the-fixture-format)
- [What the corpus covers](#what-the-corpus-covers)
- [Key design decisions](#key-design-decisions)
- [Error and boundary cases](#error-and-boundary-cases)
- [Integration](#integration)
- [Testability](#testability)
- [Open questions and deferred work](#open-questions-and-deferred-work)

## Goal and scope

A playbook can now leave the compiler, and one day more than one runtime will
read it. Nothing yet says whether two runtimes agree. This component is that
statement: **language-neutral fixtures every runtime must reproduce**, each a
playbook and the conversation a driver must be able to hold with it.

In scope:

- the fixture format, as data any language can read;
- **readable fixtures** — a document a reader must refuse, and one it must accept;
- **playable fixtures** — a session a runner must reproduce;
- the C# harness for the readable half, which runs against today's reader.

Out of scope, and deferred with reason: the harness for the playable half, which
needs a runner to run anything at all
([C2](https://github.com/pengzhengyi/dialoguedown/issues/297)).

This note assumes the vocabulary of the
[architecture note](./Dialogue%20Runtime%20Architecture.md) — *playbook*,
*driver*, *runner*, *command*, *event* — and does not restate it.

### Why the fixtures come before the runner

A corpus written **after** a runner is a mirror: the natural way to produce an
expected result is to run the fixture and accept what comes back, which proves
only that the runner agrees with itself. That is the gap
[inkjs](https://github.com/y-lohse/inkjs) lives with — it tracks the C# runtime by
hand, and drift surfaces as user bug reports.

Written **before**, the same file is a specification. The sessions here are
hand-authored from the design, so when C2 runs them the corpus is asking a real
question.

The cost is honest and worth naming: the message vocabulary below is C2's
protocol, so this note settles part of C2's design surface early, as data rather
than as C# types. That direction is deliberate — it is the same principle as the
playbook being a designed contract rather than a dump of the compiler's graph.

## Functionality checklist

- [x] A fixture format that a runtime in any language can read without a compiler.
- [x] Purpose-built source scripts, one construct each, committed beside their
      compiled playbooks.
- [x] Readable fixtures covering every refusal the reader makes, and the
      acceptances it must not refuse.
- [ ] Playable fixtures covering speech, succession, choices, conditions,
      branches, jumps, effects, and queries.
- [x] A C# harness that runs the readable fixtures today.
- [ ] A documented shape for the playable harness, so C2 has an acceptance suite
      waiting rather than a corpus to write afterward.

## Where the corpus lives

At the repository root, in `conformance/`, beside `schema/`:

```text
conformance/
  README.md                     what a port is expected to do with this
  readable/                     can a reader load this document at all
    unknown-requires/
      source.dialogue.md        what the document was compiled from
      playbook.json             that compile, then broken in one deliberate way
      fixture.json              the claim: which verdict, and why
    …
  playable/                     does a runner hold the same conversation
    a-player-choice/
      source.dialogue.md        the script, so the fixture stays maintainable
      playbook.json             compiled, committed, regenerated on demand
      fixture.json              the hand-authored session
    …
```

Every case is a directory whose `fixture.json` is the entry point, in both halves,
so a port writes one loader rather than two.

Both halves ship the source their playbook came from. A `playable/` case's
playbook is exactly what that source compiles to. A `readable/` case's is not:
its playbook is that compile with one deliberate edit, because no script compiles
to a broken playbook — a compiler will not emit an unknown capability or a
dangling node reference. The source is there so a reviewer reads a dialogue
rather than a hundred lines of JSON, and `because` names the edit.

The two directories name the **dimension a fixture probes** — can it be read, and
does it play the same way — so each holds both verdicts. `readable/` covers both
the documents a reader must refuse and the ones it must accept, and the verdict
is a per-fixture field. `readable` is the term the code already uses
(`PlaybookSupport.NewestReadableVersion`).

Not under `tests/`, which holds C# projects. The corpus is **data owned by the
format**, and a TypeScript or Rust port must be able to consume it without
building anything of ours. `schema/` set that precedent for C1; this follows it.

## The fixture format

### A playable fixture

A fixture is one **session**: the messages a driver sends, interleaved with what
the runtime must reply, in the order they occur.

```json
{
  "name": "a false condition marks a player option unavailable",
  "playbook": "playbook.json",
  "session": [
    { "expect": { "resolve": ["IsCurious"] } },
    { "send": { "supply": { "IsCurious": false } } },
    { "expect": { "said": { "speaker": "Alice", "speech": "My favorite color is red." } } },
    { "send": "continue" },
    { "expect": { "asked": [
        { "label": "Ask about the inn", "available": false },
        { "label": "Say nothing", "available": true }
      ] } },
    { "send": { "choose": 1 } },
    { "expect": { "ended": {} } }
  ]
}
```

Read top to bottom, cause sits beside effect. `send` and `expect` are the verbs
of [expect(1)](https://core.tcl-lang.org/expect/), and they are asymmetric on
purpose: `expect` is the asserting side's word, so a fixture always speaks as the
driver and never has to say whose turn it is.

### What a driver sends

Each `send` is one message from the driver. The vocabulary is the protocol's own,
so a fixture reads as the conversation it replays:

| `send` | Means |
| --- | --- |
| `"continue"` | `Continue` — proceed past what was just said |
| `{ "choose": n }` | `Choose(n)` — take the option at position `n` |
| `{ "supply": { … } }` | `Supply(answers)` — here is what the world says |
| `{ "start": "the-inn" }` | `Start(anchor)` — begin somewhere other than the top |
| `"describe"` | `Describe()` — ask where the run stands |

A session with no `start` begins at the playbook's `entry`.

`choose` is **zero-based**, and indexes the options **as just offered, in the
order offered** — not the node's outgoing edges, which can differ because
unavailable options are still shown. Zero-based keeps it the only convention in
the format: node identifiers are already gapless from zero, and the options are a
JSON array. A menu that reads `1)` to a player is the shell's presentation, and
the shell translates.

### What a runtime must reply

Each `expect` is one message the runtime must produce next.

| `expect` | Asserts |
| --- | --- |
| `said` | `speaker` (the name, never the index) and the `speech` — see below |
| `asked` | the options offered, each a `label` and whether it was `available` |
| `performed` | the effect, as the playbook names it |
| `resolve` | the keys the runtime asked the world about |
| `invalidated` | an offered option that stopped being available |
| `ended` | the run finished |

A speaker is named, not numbered: the speaker table's order is an encoding detail,
and the anonymous default speaker simply has no name.

Interleaving removes a redundancy the earlier two-list sketch carried. An `asked`
entry no longer records which option was taken, because the very next `send` says
so. One fact, one place.

### Speech and labels are fragments

Two places in a playbook hold prose: a line's `speech` and an option's `label`.
Both are lists of **fragments** — plain text, styled runs, breaks, tags, queries,
and custom commands. Fragments are what the core emits; turning them into
something a player sees is the shell's job, and a terminal, a browser, and Godot
each do it differently. So the fragments are the canonical assertion, **written
exactly as the playbook serializes them**:

```json
{ "expect": { "said": { "speaker": "Alice", "speech": [
    { "kind": "text", "text": "My key is " },
    { "kind": "styled", "style": "bold", "children": [
        { "kind": "text", "text": "rusty" } ] },
    { "kind": "text", "text": "." }
] } } }
```

Reusing the playbook's vocabulary verbatim — the `kind` discriminator, `styled`
with nested `children`, `bold` and `italic` — is the point. A port already reads
fragments to load a playbook, so conformance adds no second naming scheme to
learn, implement, or keep in sync.

Most fixtures are not about styling, and reading that for a fixture about
conditions is a poor trade. So either field may instead be written as a plain
string, which asserts the **flattened** form:

```json
{ "expect": { "said": { "speaker": "Alice", "speech": "My key is rusty." } } }
```

One field, two forms: **an array asserts the fragments, a string asserts the
flattening**. A string is a deliberately weaker assertion, chosen for
readability, so any fixture whose subject *is* styling or interpolation writes
the array. Because the string form is a projection, the projection is specified:
concatenate each fragment's plain text, drop style markers, and substitute
resolved queries. That is deterministic here, since a fixture supplies its own
answers.

The same two forms apply to an option's label, where the flattened form is the
common case and keeps the option readable:

```json
{ "expect": { "asked": [
    { "label": "Take the east road", "available": true },
    { "label": [ { "kind": "text", "text": "Ask the guide first " },
                 { "kind": "tag", "name": "cautious" } ], "available": false }
] } }
```

Neither form carries a node reference. The runtime's `Transcript` puts a
`nodeRef` on every entry, but a node reference is a **position**, so an assertion
carrying one would change whenever the compiler renumbered — exactly the churn
this corpus must not have. What is left is semantic: who spoke, what was offered,
what was taken.

### A readable fixture

The readable half asks a smaller question — can a reader load this at all — so a
fixture states the document and the verdict:

```json
{
  "name": "an unknown required capability is refused",
  "playbook": "playbook.json",
  "verdict": "refuse",
  "because": "requires 'detour', which no version-0 runtime offers"
}
```

`verdict` is `refuse` or `accept`. A refusal's message is not asserted: every
runtime should explain itself in its own language, and pinning English would make
the corpus untranslatable. `because` documents the fixture for a human reading it.

This half is not made redundant by `schema/playbook-0.schema.json`, and measuring
that was worth the trouble: **seven of the nine refusals shipped are valid by the
schema.** A schema constrains shape — `entry` is a non-negative integer — but not
meaning, so it cannot know there are only two nodes to point at, which versions a
build reads, or that a node's id must equal its position. Only the type error and
the truncated file are its to catch. Conversely, every case the corpus *accepts*
must also validate, or the format's two specifications disagree; CI checks that.

## What the corpus covers

Fixtures are **purpose-built and minimal — one construct each**, so a failure
names the construct rather than a script. The shipped `examples/` are broad and
make good regression material, but a failure in one says little about what broke.

| Fixture | Asks |
| --- | --- |
| Linear speech | Does one line follow another, and does the run end? |
| A player choice | Is the menu offered, and does a choice move where it should? |
| An unavailable option | Is a failing option **shown but unavailable**, not hidden? |
| A conditional line | Is a line skipped without ending the run? |
| A conditional block | Are the arms tried in the order written? |
| A jump | Does a divert transfer without returning? |
| An effect | Is a control block's effect performed, and reported? |
| A query in speech | Is `Resolve` raised, and the supplied answer spoken? |
| Styled speech | Do fragment boundaries and styles survive intact? |
| Ordered and unordered choices | Is a menu's stated order honored where it is stated? |

The unavailable-option fixture matters more than its size suggests: showing a
failing option rather than hiding it is a deliberate decision, and a port is
likelier to get it wrong than to get speech wrong.

## Key design decisions

### F1 — The corpus is data, not a test project

Fixtures are JSON at the repository root. A harness is a consumer, not the owner.
Anything a port cannot read without building our C# is not conformance material.

JSON rather than a session-log mini-language of our own: a bespoke format would
need a parser in **every** port before a single fixture ran, would have to escape
dialogue that happens to start with a marker character, and — decisively — would
itself need a specification and conformance tests. The corpus exists to remove
ambiguity between implementations; hand-rolling a language would reintroduce it
one level down. Readability is bought by the interleaved session instead.

### F2 — A fixture carries a playbook, not a script

A runtime has no compiler. The playbook is the artifact it loads, so that is what
a fixture supplies. The source script is committed beside it, so the fixture stays
maintainable and reviewable, and the playbook is regenerated from it — never
hand-edited.

### F3 — A fixture is a session, not a transcript

The alternative was a list of driver messages plus the transcript they produce.
Two lists read as two disjoint documents, and the reader has to interleave them
mentally to see one conversation — a real cost in a file humans author and review.

Interleaving also changes what conformance *means*, and for the better:

| Shape | Asserts |
| --- | --- |
| Transcript | the same story came out |
| Session | the same conversation happened |

A fold over the event stream cannot see a runner that emits `Choices` before
`Speech`, or asks `Resolve` for the wrong keys, or asks too eagerly. A session
can, and it has somewhere to put `describe` — which a transcript, having no slot
for a question, could not express at all.

The risk is that a stricter shape rejects a port that legitimately batches
messages differently. That is a feature: it forces C2 to *state* whether batching
is allowed instead of leaving it to be discovered when a port diverges.

### F4 — Speech and labels are the playbook's fragments

Rendering lives in the shell, so fragments are what the core is accountable for,
and a fixture writes them exactly as the playbook serializes them rather than in
a vocabulary of its own — a second naming scheme for one concept would need its
own tests and its own drift to police.

The plain-string form exists because most fixtures are not about styling and
should stay readable. Making it the *same* field rather than a second one means
the two can never be supplied together, and keeps `label` named `label`.

### F5 — A refusal asserts the verdict, not the wording

A runtime must refuse the same documents. It need not refuse them in English.

That leaves a hole: a document refused for an *accidental* reason still passes.
The readable half closes it with a **baseline**: one accepted case that nothing is
wrong with, and every refusal is that same document with exactly one field
changed. The baseline passing proves the rest is sound, so a refusal can only be
about the field its case changed. The reason is pinned without a word of any
message being asserted, and `because` names the change for a reader.

### F6 — Minimal fixtures over realistic ones

One construct per fixture. Realistic scripts belong in `examples/`, and their
playbooks are already pinned by C1's goldens.

## Error and boundary cases

| Case | Behavior |
| --- | --- |
| The runtime replies something other than the next `expect` | Fail, reporting both messages — this is the divergence the corpus exists to catch |
| A runner asks for input the session does not answer next | Fail, naming the divergence — a silent skip would hide it |
| The session ends before the run does | Fail: the fixture is incomplete, which is a fixture bug worth surfacing |
| The run ends before the session does | Fail, for the same reason |
| A `said` differs in one field | Fail, reporting the entry and the field rather than the whole document |
| A fixture's playbook does not load | Fail as a fixture bug, distinct from a conformance failure |
| A readable fixture whose document is not valid JSON | Still a refusal; the corpus does not care why |

## Integration

| Seam | Change |
| --- | --- |
| `conformance/` | New root folder, alongside `schema/`, with a `README.md` a port starts from |
| `schema/fixture-0.schema.json` | The fixture format's own schema, published beside the playbook's, so a fixture is checked in an editor as it is hand-authored |
| C# harness | A test project reads `conformance/readable/` and runs it through `PlaybookReader`; no new dependency |
| C2 | Inherits the playable fixtures as its acceptance suite, and implements the harness that runs them |
| C4 | The `ddown play` REPL sends and receives the same messages, so a session and a REPL transcript are one shape — `--replay <fixture>` makes the REPL a harness |
| C5b | The TypeScript runner is held to the same corpus, which is the whole reason the fixtures are language-neutral |
| CI | The readable harness runs with the existing suite; every case the corpus accepts is also validated against the schema, so the format's two specifications cannot drift apart |

## Testability

The corpus is itself test material, so the question is what tests *it*.

| Level | What it covers |
| --- | --- |
| Harness unit | The harness fails when it should — a wrong verdict, a missing playbook, a malformed fixture |
| Readable corpus | Every refusal **the reader** makes has a case, and every acceptance does too. C1's boundary table also lists a duplicate speaker id, which the *writer* asserts before emitting, so no document a reader could be handed exercises it |
| Fixture integrity | Every fixture validates against `schema/fixture-0.schema.json` in CI, which is what holds the hand-authored playable half together until C2 can run it. Every case ships a fixture, a playbook, and a source. A `playable/` playbook is what its source compiles to; a `readable/` one deliberately is not, so only the former is regenerated and compared |

That last one is the guard against a corpus rotting: a committed playbook that no
longer matches its source is a fixture asserting yesterday's format.

## Open questions and deferred work

- **`describe` has a slot but no fixtures.** The format accommodates the query
  half so it does not have to be retrofitted, but what a `describe` reply
  contains is C2's to settle — writing fixtures now would mean designing C2's
  query surface from the outside. The line debugger is the consumer that will
  force the shape.
- **Which fragment kinds survive a run is C2's to settle.** A fixture writes the
  playbook's fragments verbatim, but the runtime cannot emit all of them
  unchanged: a `query` fragment must become something else once `Supply` answers
  it, and whether `tag` and `custom-command` pass through to the shell or surface
  as their own events is a runner decision. The rule here — reuse the vocabulary,
  and expect it to differ only where the runtime resolves something — holds
  either way, so fixtures that lean on those kinds wait for C2.
- **Random choice has no fixture yet.** Pinning a draw needs the entropy decision
  C2 owns — a specified generator, or values the host supplies. Deferred until
  that is settled, and called out here rather than quietly omitted.
- **A rendered view of a session** — `ddown conformance show <fixture>` printing a
  session as prose — would give human readability with no parser in any port,
  since it is generated and never authored. Worth doing once fixtures exist.
- **The playable harness is deferred to C2**, which is the only component that can
  run it.

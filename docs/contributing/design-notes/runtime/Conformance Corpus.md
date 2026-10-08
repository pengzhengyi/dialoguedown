# Conformance Corpus

> [!NOTE]
> Status: **implemented**. Language-neutral fixtures every runtime must reproduce:
> `readable/` cases a playbook reader must accept or refuse, and `playable/`
> sessions a runner must hold. The C# reader and [runner](./Runner.md) run both
> halves; a playable case the runner cannot play yet is reported as not yet
> playable rather than skipped.

The corpus layout, the fixture files, and how to add a case are documented beside
the fixtures in [`conformance/README.md`](https://github.com/pengzhengyi/dialoguedown/blob/main/conformance/README.md), and the
fixture format is specified by
[`schema/fixture-0.schema.json`](https://github.com/pengzhengyi/dialoguedown/blob/main/schema/fixture-0.schema.json). This note
records the decisions behind them.

## Table of contents

- [The session vocabulary](#the-session-vocabulary)
- [What the corpus covers](#what-the-corpus-covers)
- [Key design decisions](#key-design-decisions)
- [Error and boundary cases](#error-and-boundary-cases)
- [Testability](#testability)
- [Open questions and deferred work](#open-questions-and-deferred-work)

## The session vocabulary

A playable fixture is one **session**: the messages a driver sends interleaved with
what the runtime must reply, in order.

```json
{ "expect": { "said": { "speaker": "Alice", "speech": "Which way?" } } },
{ "send": "next" },
{ "expect": { "offer": { "ordered": false, "options": [
    { "label": "Go east", "available": true },
    { "label": "Go west", "available": true } ] } } },
{ "send": { "choose": 0 } }
```

| `send` | Means |
| --- | --- |
| `"next"` | `Next` — proceed past what was just said |
| `"done"` | `Done` — the effect just asked for has been carried out |
| `{ "failed": "…" }` | `Failed(explanation)` — the effect could not be carried out |
| `{ "choose": n }` | `Choose(n)` — take the option at zero-based position `n` among those just offered |
| `{ "supply": { … } }` | `Supply(answers)` — what the world says |
| `{ "start": "the-inn" }` | `Start(anchor)` — begin somewhere other than `entry` |
| `"describe"` | `Describe()` — ask where the run stands |

| `expect` | Asserts |
| --- | --- |
| `said` | the `speaker` name (absent for the anonymous default speaker) and the `speech` |
| `continued` | the `speech` after a command, going on with the line a `said` opened; it names no speaker |
| `offer` | the menu offered: whether it is `ordered`, and its `options`, each a `label` and whether it is `available`. A bulleted menu's options match in any order |
| `perform` | the effect the runtime asks the host to carry out, as the playbook names it |
| `resolve` | the keys the runtime asks the world about |
| `invalidated` | an offered option that stopped being available |
| `ended` | the run finished |
| `refused` | the `reason`, from the runner's [closed set](./Runner.md#refusals) |

A session with no `start` begins at the playbook's `entry`. `speech` and `label` are
written either as an **array** — the playbook's fragments, verbatim — or as a
**string**, which asserts the flattening defined by
[Speech as Plain Text](./Speech%20as%20Plain%20Text.md). The C# runner takes
`next`, `done`, and `failed`; a case that sends anything else is not yet playable.

## What the corpus covers

Fixtures are **minimal — one construct each** — so a failure names the construct
rather than a script.

| Playable case | Asks | Plays in the C# runner |
| --- | --- | --- |
| `linear-speech` | Does one line follow another, and does the run end? | yes |
| `styled-speech` | Do fragment boundaries and styles survive intact? | yes |
| `a-jump` | Does a jump transfer without returning? | yes |
| `an-effect` | Is an effect asked for, and waited on before the run goes past it? | yes |
| `a-failed-effect` | Does the run stand still, so a retry lands and an advance cannot? | yes |
| `a-next-while-waiting` | Is `next` refused while the host is carrying out an effect? | yes |
| `a-command-too-late` | Is a command after the end refused with the reason the session names? | yes |
| `a-player-choice` | Is the menu offered, and does a choice lead into its arm? | not yet |
| `a-divert-option` | Does a menu written as jumps (`- => [Label](#anchor)`) lead where it says? | not yet |
| `an-unavailable-option` | Is a false option **shown but unavailable**, not hidden? | not yet |
| `a-conditional-line` | Is a line skipped without ending the run? | not yet |
| `a-conditional-block` | Are the arms tried in the order written? | not yet |
| `a-query-in-speech` | Is `resolve` raised, and the supplied answer spoken? | not yet |

The `readable/` half covers every refusal the reader makes — version, capability,
node position, the four dangling references, and the
[reader rules](./Playbook%20Reader%20Rules.md) — plus the documents it must accept.

## Key design decisions

### F1 — The corpus is data, not a test project

Fixtures are JSON at the repository root, beside `schema/`, not under `tests/`. A
harness is a consumer, not the owner: a TypeScript or Rust port must be able to run
them without building anything of ours. JSON rather than a session mini-language,
because a bespoke format would need a parser and a specification of its own in every
port — the ambiguity the corpus exists to remove.

### F2 — A fixture carries a playbook, not a script

A runtime has no compiler, so a fixture supplies the playbook it loads. The source
script is committed beside it, so a reviewer reads a dialogue rather than JSON, and a
test recompiles every playable source and compares it to the committed playbook. A
`readable/` refusal is an accepted document with one deliberate edit, and its source
opens with a `broken:` block showing that edit.

### F3 — A fixture is a session, not a transcript

| Shape | Asserts |
| --- | --- |
| Transcript | the same story came out |
| Session | the same conversation happened |

A fold over the event stream cannot see a runner that reports `Offer` before `Said`,
asks `Resolve` for the wrong keys, or asks too eagerly. Interleaving also removes a
redundancy: an `offer` entry does not record the pick, because the next `send` says
so. The stricter shape forces the runtime design to *state* whether batching is
allowed rather than leave it to be discovered when a port diverges.

### F4 — Speech and labels are the playbook's fragments

Rendering lives in the host, so fragments are what the core is accountable for, and
a fixture writes them exactly as the playbook serializes them — no second naming
scheme to keep in sync. The string form exists because most fixtures are not about
styling; it is the same field rather than a second one, so the two can never be
supplied together. A fixture whose subject *is* styling or interpolation writes the
array. Neither form carries a node reference: a position churns whenever the
compiler renumbers.

### F5 — A refusal asserts the verdict, not the wording

A runtime must refuse the same documents, not refuse them in English. To stop a
document refused for an *accidental* reason from passing, every refusal has an
accepted document one edit away — `baseline/` for the line-level cases, or the case's
own compiled source — so a refusal can only be about the edit its case made. A runner
refuses a **command**, not a document, so the reason travels on the refusal itself as
a value from a closed set, and `expect` asserts it.

### F6 — Minimal fixtures over realistic ones

One construct per fixture. Realistic scripts belong in `examples/`, whose playbooks
are pinned by the goldens.

### F7 — A verdict gathers every reason it carries

An outcome carries a **list** of reasons, so a contributor sees every divergence in
an entry at once rather than fixing one and re-running. The gravest verdict wins, and
reasons gather at that verdict in the order the checks ran; a reason a graver verdict
outranks is dropped. The run still stops at the first entry that does not conform,
because a send advances the run and later entries would be judged against a state
the fixture never described. What the build has **yet to learn** — node kinds, sends,
and claims nothing checks — is gathered before the run starts, so one run names
everything a case needs. The verdict is **not yet playable**, matching
`RefusalReason.UnplayableNode` and the `playable/` folder.

## Error and boundary cases

| Case | Behavior |
| --- | --- |
| A fixture's playbook does not load | Fails as a fixture bug, naming the case and the file |
| A fixture is malformed, or a case lacks a fixture, playbook, or source | Fails naming the case |
| A `refused` names a reason the protocol does not give | Fails as a fixture bug; the schema closes the set |
| A readable document that is not valid JSON | Still a refusal |
| The runtime replies something other than the next `expect` | Fails, reporting both messages |
| A runner asks for input the session does not answer next | Fails, naming the divergence |
| The session ends before the run, or the run before the session | Fails, saying which |
| A `said` differs in several fields | Fails, reporting every field that differs |

## Testability

| Level | What it covers |
| --- | --- |
| Harness unit | The harness fails when it should — a wrong verdict, a missing playbook, a malformed fixture |
| Readable corpus | Every refusal the reader makes has a case, and every acceptance does too |
| Fixture integrity | Every fixture validates against `schema/fixture-0.schema.json` in CI; every case ships a fixture, a playbook, and a source |
| Source integrity | Every playable source recompiles to its committed playbook; every readable refusal's `broken:` block is well formed, and the script below it compiles to an accepted document that differs from the committed one |
| Schema agreement | Every accepted playbook validates against `schema/playbook-0.schema.json` |

The source comparison lives in `DialogueDown.Tests`, which owns the compiler; the
readable harness in `DialogueDown.Playbook.Tests`; the playable harness in
`DialogueDown.Runtime.Tests`. `DialogueDown.Conformance` finds cases and reads their
files for all three.

## Open questions and deferred work

- **`describe` has a slot but no fixtures.** What a `describe` reply contains is the
  runner's to settle; the line debugger is the consumer that will force the shape.
- **Which fragment kinds survive a run.** A `query` fragment must become something
  else once `supply` answers it, and whether `tag` and `custom-command` pass through
  or surface as their own events is a runner decision.
- **No case pins a numbered menu's order yet.** An `offer` says which kind a menu
  is, and a bulleted menu's options match in any order because shuffling is the
  host's; the one numbered menu in the corpus waits on a menu that reads the world.
- **Random choice has no fixture.** Pinning a draw needs the entropy decision the
  [architecture note](./Dialogue%20Runtime%20Architecture.md#open-questions-and-deferred-work)
  owns.
- **A rendered view of a session** — printing a fixture as prose — would give
  readability with no parser in any port.

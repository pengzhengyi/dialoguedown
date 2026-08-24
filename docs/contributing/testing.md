# How this project is tested

A map of the kinds of tests DialogueDown runs, what each one is *for*, and where
to add yours. Read this before writing your first test — it should save you from
adding an example-based test where a different kind of test would say more.

For the commands themselves — how to run the suite, collect coverage, and what CI
gates on — see the
[contribution guide](https://github.com/pengzhengyi/dialoguedown/blob/main/CONTRIBUTING.md).
This page covers *which* test to write; that one covers *how* to run it.

## Table of contents

- [The shape of the suite](#the-shape-of-the-suite)
- [Where the tests live](#where-the-tests-live)
- [The kinds of test, and what each is for](#the-kinds-of-test-and-what-each-is-for)
  - [Example-based tests](#example-based-tests)
  - [Property tests](#property-tests)
  - [Golden tests](#golden-tests)
  - [Round-trip tests](#round-trip-tests)
  - [Architecture tests](#architecture-tests)
  - [Corpus gates](#corpus-gates)
  - [Conformance tests](#conformance-tests)
  - [Browser tests](#browser-tests)
  - [Repository infrastructure tests](#repository-infrastructure-tests)
- [Which kind should I write?](#which-kind-should-i-write)
- [Under consideration](#under-consideration)

## The shape of the suite

Roughly **3,900 .NET tests** and **900 frontend tests**, weighted heavily toward
fast unit tests, with a thin layer of **32 browser spec files** at the top. The
whole .NET suite runs in about 20 seconds, which is deliberate: it is meant to be
run constantly.

Most of that count is ordinary example-based unit tests. The interesting part is
the smaller set below them — property tests, goldens, architecture tests, and
corpus gates — because those ask questions an example-based test cannot, and they
are the reason this page exists.

## Where the tests live

| Project | Covers |
| --- | --- |
| `DialogueDown.Tests` | The compiler core — parsing, transpiling, desugaring, semantics, the graph, and the playbook. The bulk of the suite. |
| `DialogueDown.Visualization.Tests` | The report's projections: how each compiler stage becomes something to look at. |
| `DialogueDown.Visualization.Live.Tests` | The served report — the file watcher, the session, and saving back to disk. |
| `DialogueDown.Cli.Tests` | The `ddown` commands, their options, and their exit codes. |
| `DialogueDown.ConfigurationLoader.Tests` | Reading `dialogue.toml`. |
| `DialogueDown.Playbook.Tests` | The published playbook format, its schema, and the conformance harness that runs the corpus. |
| `DialogueDown.Architecture.Tests` | The boundaries between all of the above. |
| `src/DialogueDown.Visualization/web` | The report client: unit tests beside the source, browser tests under `e2e/` and `e2e-live/`. |

## The kinds of test, and what each is for

### Example-based tests

**One input, one expected output.** The baseline, and most of the suite.

This is the right way to *specify* behavior: a test names a case, shows the input,
and states what should come out. Start here unless one of the kinds below answers
a question this one cannot.

### Property tests

**One rule, a thousand generated inputs.**

An example-based test can only cover the cases someone thought to write. A
property test states a rule that must hold for *every* script and then generates
scripts to try to break it — that every node's span addresses text that exists,
that a child's span sits inside its parent's, that compiling never throws.

Written with [CsCheck](https://github.com/AnthonyLloyd/CsCheck) in
`tests/DialogueDown.Tests/compilation/CompilerPropertyTests.cs`. The
[contribution guide](https://github.com/pengzhengyi/dialoguedown/blob/main/CONTRIBUTING.md)
explains when to add one and how to keep the generator honest.

### Golden tests

**Pin a large output so a change to it is a reviewable diff.**

The playbook each shipped example compiles to is committed under
`tests/DialogueDown.Tests/emission/goldens/`. Assertions express a 300-line JSON
document badly; a diff expresses it well. When output changes, you accept the new
golden — and the pull request shows exactly what moved.

A golden answers *"did this example's output change?"*. It cannot tell you
whether the change was **right**, so read the diff.

### Round-trip tests

**Write it, read it back, and check nothing changed.**

A playbook is a persisted artifact — the one way out of the compiler — so a
writer's work survives only if what was written reads back as what it was. For
every generated script that compiles, `PlaybookRoundTripTests` writes a playbook,
reads it with `PlaybookReader`, and writes it again: the two renderings must
match.

Equality is taken over the serialized JSON, not over the document. A
`PlaybookDocument` holds its nodes in `ImmutableArray`, whose record equality
compares the underlying array **by reference**, so two structurally identical
documents are never equal. Re-serializing compares what a file would actually
hold.

A round trip needs no oracle beyond the input itself, and a counterexample is
directly a bug report: either the reader lost something the writer emitted, or
the writer emitted something the reader cannot express.

### Architecture tests

**Guard the shape of the codebase, not its behavior.**

Layering erosion compiles fine and passes every unit test. These catch it:

- the core never depends on the CLI, the visualization, or any engine;
- the Dialogue AST stays immutable, so no later stage can rewrite what an
  earlier one produced;
- no core type grows into a God class, and no namespace flattens into a list.

They live in `DialogueDown.Architecture.Tests` and run in seconds.

### Corpus gates

**Keep the shipped examples and the docs honest about each other.**

The examples under `examples/` are the project's shop window, the input to the
goldens, and what the demo page shows. Two tests hold that together:

| Test | Asks |
| --- | --- |
| `ExampleConstructCoverageTests` | Does every construct the compiler models appear in some example? |
| `DemoPageStagesTests` | Does the demo page advertise exactly the stages the report renders? |

Both read objects rather than rendered text, and both have caught real drift —
the demo page went a whole stage out of date before the second one existed.

Add a construct to the language and the first test fails until an example uses
it. That is deliberate: an undemonstrated construct is a documentation hole, not
a compiler bug.

### Conformance tests

**Prove a runtime agrees with the specification, not with us.**

The corpus under [`conformance/`](../../conformance/README.md) is language-neutral
data: a runtime in any language runs it without building anything of ours. Each
case is a folder whose `fixture.json` states what must happen, beside the playbook
it happens to and the source that playbook was compiled from.

| Half | Asks | Run by |
| --- | --- | --- |
| `readable/` | Can a reader load this document at all? | `ReadableConformanceTests`, today |
| `playable/` | Does a runner hold the same conversation? | the runtime, when it lands |

The fixtures are **hand-authored from the design**. That is the whole point: a
corpus recorded from an implementation can only prove that implementation agrees
with itself, which is the gap [inkjs](https://github.com/y-lohse/inkjs) lives
with. Written first, the same file is a specification.

Two properties are worth knowing before adding a case:

- **One accepted `baseline`, and every refusal is that document with exactly one
  field changed.** A diff between any case and the baseline is the single line the
  case is about, and because the baseline passes, a refusal can only be caused by
  the field its case touched.
- **A refusal's message is never asserted.** Every runtime should explain itself in
  its own language; `because` is for the human reading the file.

This is not made redundant by the schema. Seven of the nine refusals shipped are
**valid** by `schema/playbook-0.schema.json`: a schema constrains shape, so it
cannot know there are only two nodes to point at, or which versions a build reads.

`CorpusIntegrityTests` and `PlayableCaseTests` keep the corpus itself honest —
every case ships its three files, and every playable playbook is recompiled from
its source and compared.

### Browser tests

**Prove the report works in a real browser.**

Two suites, and the difference matters:

| Suite | Runs against | Use it for |
| --- | --- | --- |
| `e2e/` | A static `file://` export | Rendering, interaction, accessibility. Fast. |
| `e2e-live/` | The real .NET server | Hot reload, saving, the Explorer, anything needing a server. |

Seven `e2e/` specs also assert **no accessibility violations** with
[axe](https://github.com/dequelabs/axe-core), so a keyboard- or screen-reader
regression fails the build rather than waiting for a report.

Browser tests are the slowest and most fragile thing here. Push a behavior down
into a unit test whenever it can live there.

### Repository infrastructure tests

**Check that the project's own tooling still says what it means.**

Plain Node tests under `e2e-live/*.test.mjs` assert that documented commands stay
correct, that caches stay out of the repository, and that the shipped client stays
under its size budget. They exist because a stale command in a contributor guide
fails silently — nobody notices until someone new follows it.

## Which kind should I write?

Start from what would go wrong, not from the list:

| If you are… | Write |
| --- | --- |
| specifying a behavior | an example-based test |
| stating a rule that holds for *all* inputs | a property test |
| changing a large structured output | let the golden diff show it |
| adding a language construct | an example that uses it, plus the usual unit tests |
| changing what the playbook carries | check the round-trip property still holds |
| moving code between projects | check the architecture tests still pass |
| touching the report's UI | a unit test if possible, a browser test if not |

When two kinds both fit, prefer the faster one. The suite is run constantly;
every second added is paid many times.

## Under consideration

Tracked, not yet adopted, each with a reason it is waiting:

| Kind | Would catch | Status |
| --- | --- | --- |
| **Mutation testing** | Tests that execute code without meaningfully asserting on it — coverage's blind spot. | Waiting on Stryker.NET support for xUnit v3 on the Microsoft Testing Platform. Mutating the *format* rather than the code needs no such support, and the conformance corpus already does it by hand ([#336](https://github.com/pengzhengyi/dialoguedown/issues/336)). |
| **A Law of Demeter fitness test** | Code reaching through one object to talk to another. | Deferred until the runtime introduces stateful object graphs ([#217](https://github.com/pengzhengyi/dialoguedown/issues/217)). |

Two kinds are deliberately **absent** rather than pending. Benchmarks wait for a
performance requirement worth defending — the compiler is fast enough that a
regression would have to be enormous to matter. Fuzzing waits for untrusted
input; a script is written by the person running the compiler, so a crash is a
bug report, not a breach.

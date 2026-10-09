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
- [How a test reads](#how-a-test-reads)
  - [The fixture is a named scenario](#the-fixture-is-a-named-scenario)
  - [One mother per type](#one-mother-per-type)
  - [Assertions say what they claim](#assertions-say-what-they-claim)
  - [What may be shared](#what-may-be-shared)
  - [Names](#names)
- [Coverage that grows with the feature](#coverage-that-grows-with-the-feature)
- [Under consideration](#under-consideration)

## The shape of the suite

Thousands of .NET and frontend tests, weighted heavily toward fast unit tests,
with a thin layer of browser specs at the top. The whole .NET suite runs in
seconds, which is deliberate: it is meant to be run constantly.

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
| `DialogueDown.Playbook.Tests` | The published playbook format, its schema, and the readable half of the corpus. |
| `DialogueDown.Runtime.Tests` | The runner — stepping, positions, the protocol — and the harness that plays the corpus against it. |
| `DialogueDown.Conformance` | Not tests: the reader both halves of the corpus load their cases with. |
| `DialogueDown.TestSupport` | Not tests: what more than one suite needs — the concrete types under a base, the shipped examples, and temporary files and folders. |
| `DialogueDown.Conformance.Tests` | That reader, and the corpus's own integrity. |
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
reads it with `PlaybookReader`, and writes it again: the two JSON texts must match.
Comparing text rather than objects also catches a change in how a field is spelled
or ordered. The playbook records compare by value too, so a test asking about the
model can compare objects directly.

A round trip needs no oracle beyond the input itself, and a counterexample is
directly a bug report: either the reader lost something the writer emitted, or
the writer emitted something the reader cannot express.

### Architecture tests

**Guard the shape of the codebase, not its behavior.**

Layering erosion compiles fine and passes every unit test. These catch it, using
NetArchTest.eNhancedEdition:

| Boundary | Rule |
| --- | --- |
| Assemblies | The core depends on no CLI, visualization, configuration-loader, presentation, or engine package. `Visualization.Live → Visualization → core`, never back. The loader depends only on the core and Tomlyn; the runtime only on the playbook; the playbook only on the framework. |
| Core layers | `Common`, `Configuration`, and `Diagnostics` are foundation leaves. The Dialogue AST, desugar, and semantics stay free of Markdown and the transpiler; validation never depends on the semantic model; no stage calls back into the compilation orchestrator; the graph depends only upstream. |
| Shape | The Dialogue AST stays immutable, no core type grows into a God class, and no namespace flattens into a list. |

They live in `DialogueDown.Architecture.Tests`. When you add a layer or a boundary,
extend that suite so the rule travels with the code.

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

The corpus under [`conformance/`](https://github.com/pengzhengyi/dialoguedown/blob/main/conformance/README.md) is language-neutral
data: a runtime in any language runs it without building anything of ours. Each
case is a folder whose `fixture.json` states what must happen, beside the playbook
it happens to and the source that playbook was compiled from.

| Half | Asks | Run by |
| --- | --- | --- |
| `readable/` | Can a reader load this document at all? | `ReadableConformanceTests` |
| `playable/` | Does a runner hold the same conversation? | `PlayableConformanceTests` |

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

This is not made redundant by the schema. Most refusals are **valid** by
`schema/playbook-0.schema.json`: a schema constrains shape, so it cannot know there
are only two nodes to point at, or which versions a build reads.

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

Several `e2e/` specs also assert **no accessibility violations** with
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

## How a test reads

The tests are the project's most exact documentation: they say what each part
does, and they fail when that stops being true. A reader should be able to learn
a behavior from its test without opening the code under test. So a test is
written with the same care as the code it checks, and refactored just as freely.

A test that reads well looks like this:

```csharp
[Fact]
public void Step_NextPastTheLastLine_EndsTheRun()
{
    var context = PlayContextFactory.OneLine();

    AssertEnded(Runner.Step(context, Started(context), new Next()));
}
```

The name states the behavior. The body names the scenario, does one thing, and
says what must hold. The script, the playbook it compiles to, and the checks that
make up "ended" all live behind helpers a reader can hover.

### The fixture is a named scenario

A test never assembles its fixture inline when the reader would have to decode
constructor arguments to learn the scenario. It calls a helper named for the
scenario, and the helper's `<remarks>` shows the dialogue a writer would type, so
hovering the call shows the script:

```csharp
/// <summary>One line, then the end.</summary>
/// <remarks>
/// <code>
/// Alice: Hello.
/// </code>
/// </remarks>
public static PlayContext OneLine() =>
    Of([Line(0, speaker: 0, "Hello.", next: 1), End(1)], ["Alice"]);
```

A fixture that no script could produce, such as a node with no way out, says so in
its remarks instead of inventing source.

Prefer **real dialogue** to a hand-built model. Where a suite has a `Pipeline`
and the stage under test can be reached by compiling a script, compile it and
let the test show the source as a raw string literal. Build the model by hand
only to test a stage in isolation, or to reach a shape no script produces.

### One mother per type

The type under test, and a value a reader takes in at a glance such as
`new EndNode(0)`, are built directly. Everything else comes from its type's
mother, so a change to a constructor touches one file. A test that needs a
variation says only what varies: with `with` where the record allows it, or with
an argument to the mother where it does not.

```csharp
var options = CompilerOptions.Default with { Mode = CompilationMode.BestEffort };
var gated = PlaybookNodes.Divert(target: 2, key: "Alice.HasKey");
```

The playbook records redeclare their properties get-only, so a `with` expression
cannot set them; their mothers take the varying value as an argument instead.

| What | Lives in | Example |
| --- | --- | --- |
| A scenario one test class uses | A private static method in that class | `AConditionalLine()` |
| A scenario several classes use | The suite's `support/` folder | `PlayContextFactory.OneLine()` |
| The mother for a type | The suite's `support/` folder; `DialogueDown.TestSupport` when several suites need it, with the project reference that takes | `SourceSpanFactory` |

Keep one mother per type. When two suites each have one, merge them. The runtime
and visualization suites still keep separate playbook mothers.

Name a mother for the main type it builds, with the `Factory` suffix:
`SourceSpanFactory`, `LocatedDiagnosticFactory`. The runtime's `PlaybookNodes` is
the one exception, because its `Line(…)` and `End(…)` read like the playbook they
build.

### Assertions say what they claim

A group of assertions that appears together more than once becomes a helper
named for the claim it makes. Helpers follow one form: a class named for its
subject with the singular `Assert` suffix, holding static methods, kept in the
suite's `support/` folder and imported with `using static`:

```csharp
using static DialogueDown.Tests.Support.CompilationAssert;

var result = AssertSuccess(ScriptCompilerFactory.CreateDefault().Compile(source));
```

A helper checks every invariant the inline version always checked together, so
including them makes the test stronger rather than only shorter. A helper that
narrows a type, as `AssertSuccess` narrows a result to a success, returns the
narrowed value so the test can assert more on it. Where this matters most:

- **A type check followed by field checks** (`Assert.IsType<LineNode>`, then its
  speaker, then its text) becomes one helper that checks the node and returns it.
- **JSON** is parsed and asserted by structure, not searched for a quoted
  fragment such as `"\"mode\":\"edit\""`, which passes on a match in the wrong
  place.
- **Large output** such as a whole playbook or report is pinned as a
  [golden](#golden-tests) with Verify, not checked by a dozen `Contains` calls.

Where a helper wraps a member, that member's own test calls it directly, so
the member is tested without the helper in between.

### What may be shared

Tests run in parallel and each builds its own fixture. A test may share only what
is immutable and stateless, and only when building it costs something. The
compiler stages behind `Pipeline` are the example: built once as `static readonly`
fields, because they hold no state between calls. Nothing that a test can change
is ever shared.

### Names

Name a test `Method_Scenario_ExpectedResult`. A test of a rule that holds across
the whole suite, rather than of one member, may be named as a sentence instead,
such as `EveryCatalogCodeIsDocumented`.

## Coverage that grows with the feature

A few tests here cover less than they eventually will, on purpose. The generator
behind the runtime's walk property draws only the node kinds the runner can play;
the playable half of the conformance corpus ships more cases than the runner plays.
Both are correct, and both are unfinished by design — the runtime arrives
one pass at a time, and a test that waited for all of it would test nothing until
the end.

The risk is not the gap. It is a gap that closes quietly: a later pass teaches the
runner to play choices, the generator that should now draw them carries on drawing
lines, and the property stays green while covering less than it claims. Nothing is
red, so nothing asks to be revisited.

So a test in this shape **names what it covers rather than counting it**, and
fails when that list stops being true in either direction. A case that starts
passing matters as much as one that stops, because the first is the moment the
list should have grown.

| Test | Covers | Widens when | Tells you when to widen |
| --- | --- | --- | --- |
| `PlayableConformanceTests` | The playable cases the runner can play, named one by one | The runner learns a construct that lets a whole case play | Yes — a case that starts passing fails the test until the list admits it |
| `PlaybookGen` | Lines, ends, jumps, effects, and lines that jump | Each runtime pass teaches the runner a node or edge kind | Yes — teaching the runner a kind the generator skips fails a test that names the kind |

`PlaybookGen` is held to the format the way `ExampleConstructCoverageTests` is
held to the compiler: both enumerate what exists by reflection, then fail naming
what is missing. The generator's check compares the kinds a sample of it actually
produced against the kinds the playbook format defines, so a list of what it means
to draw cannot drift from what it draws.

A kind the generator skips is listed with the reason it is skipped, and for a node
kind that reason is checked rather than believed: the test asserts the runner still
refuses one. Teaching the runner a kind therefore makes its reason false, and the
failure names the kind the generator now owes.

Both lists come out when the runtime is complete. Until then, they are an honest
statement of how far the suite reaches, and they stay honest by failing when they
stop being true.

## Under consideration

Tracked, not yet adopted, each with a reason it is waiting:

| Kind | Would catch | Status |
| --- | --- | --- |
| **Mutation testing** | Tests that execute code without meaningfully asserting on it — coverage's blind spot. | Waiting on Stryker.NET support for xUnit v3 on the Microsoft Testing Platform. Mutating the *format* rather than the code needs no such support, and the conformance corpus already does it by hand. |
| **A Law of Demeter fitness test** | Code reaching through one object to talk to another. | Deferred until the runtime introduces stateful object graphs. |

Two kinds are deliberately **absent** rather than pending. Benchmarks wait for a
performance requirement worth defending — the compiler is fast enough that a
regression would have to be enormous to matter. Fuzzing waits for untrusted
input; a script is written by the person running the compiler, so a crash is a
bug report, not a breach.

---
applyTo: "**/*.cs"
---

# C# conventions

Apply to the library, CLI, and tests. Full setup and the pull-request checklist
are in [`CONTRIBUTING.md`](../../CONTRIBUTING.md); the error model and per-stage
design live in [`docs/contributing/design-notes/`](../../docs/contributing/design-notes/README.md).

## Style

- The build enforces **StyleCop.Analyzers** and `.editorconfig`; follow what they
  flag rather than fighting them.
- **File-scoped namespaces.** `using` directives sit **outside** the namespace,
  system directives first; unused usings are a warning (IDE0005).
- Prefer **`var`** for built-in and apparent types, **expression-bodied members**
  when they fit on one line, and **pattern matching** over `is`/`as` with casts.
- **Match the construct to the complexity.** Expression-bodied members and the
  conditional (`?:`) operator are for genuinely simple, one-line logic; when a
  member's body is complex — a long or nested ternary, multiple `out`
  parameters, several branches — use a **block body with an `if` statement**
  instead. A short `if` that returns early reads better than a sprawling ternary.
- Use **LINQ** when it is a clear readability win over an explicit loop; keep it
  simple, not clever.
- Four-space indent, LF line endings, UTF-8.
- **Write self-documenting code.** Let clear names, small methods, and good types
  carry the meaning so the code reads without narration. Reserve comments for
  public-API doc comments (`///`) and the occasional *why* — intent, a non-obvious
  tradeoff, or a subtle edge case — never to restate *what* the code already says.
  Don't be afraid to comment when it genuinely aids understanding, but treat the
  urge to explain a tangled block as a signal to refactor it instead.

## Size and complexity

The **core library** (`src/DialogueDown`) enforces size and complexity guardrails
through **SonarAnalyzer.CSharp** (referenced only by the core project, so the CLI
and visualization assemblies are unaffected):

| Guardrail | Limit | Rule |
| --- | --- | --- |
| Method lines of code | 40 | S138 |
| File lines of code | 400 | S104 |
| Method parameters | 7 | S107 |
| Cyclomatic complexity | 10 | S1541 |
| Cognitive complexity | 15 | S3776 |

These are advisory **warnings**, not build errors — treat them as a prompt to
split a method, introduce a parameter object, or simplify branching. Thresholds
live in [`src/DialogueDown/SonarLint.xml`](../../src/DialogueDown/SonarLint.xml)
(SonarAnalyzer reads rule parameters only from that file); severities and the
core-only scope live in [`.editorconfig`](../../.editorconfig). The analyzer is a
build-time-only dev dependency (`PrivateAssets="all"`), so its source-available
license does not affect the library's MIT license or reach consumers.

## Design and errors

- Follow **SOLID** and **composition over inheritance**; reach for a pattern only
  when it removes real duplication or coupling.
- **Law of Demeter, scoped to behavior.** Reaching through an object to send a
  message — `active.Session.Broadcaster.Subscribe()` — couples the caller to every
  type it walks; put the method on the direct collaborator instead. Three shapes
  are explicitly *not* violations and must not be "fixed": reading across the
  immutable AST records (`node.Span.Start`) is data navigation, a fluent chain
  (`parser.Or(x).Or(y).Located()`) returns the same abstraction, and third-party
  APIs (`context.Response.Headers.Location`) follow their own shape. Enforcement is
  by review for now; an automated depth check is parked in
  [#217](https://github.com/pengzhengyi/dialoguedown/issues/217) until the runtime
  introduces stateful object graphs.
- Throw **specific, meaningful exceptions** that name the offending input and the
  expectation. Extract non-trivial validation into a small, well-named helper.
- Use the project **error model** (exception hierarchy, spans, message
  conventions) documented in the
  [design-notes README](../../docs/contributing/design-notes/README.md); component
  notes link back to it instead of redefining errors.

## Tests

- **Test-driven:** a failing test first, then the minimum code, then refactor. A
  bug fix starts with a test that reproduces it.
- **xUnit** with **NSubstitute** for mocks and **coverlet** for coverage.
- **One test file per source file**, mirroring the folder layout and named for the
  type under test (`Foo.cs` → `FooTests.cs`).
- Name tests `Method_Scenario_ExpectedResult`; cover edge and error cases, not just
  the happy path.
- Write multi-line test input as **multi-line raw string literals** (`"""…"""`),
  not single lines stitched with `\n`, so the parsed shape is visible.
- Build a type's dependencies through the shared **test factory** (Object Mother)
  so a constructor change touches one place.
- **Treat tests as code:** refactor them relentlessly, and follow the testing
  pyramid — many small unit tests, fewer integration tests, minimal end-to-end.
- **Guard architecture with tests.** Dependency-direction rules live in
  [`tests/DialogueDown.Architecture.Tests`](../../tests/DialogueDown.Architecture.Tests),
  built on **NetArchTest.eNhancedEdition** (the maintained fork of NetArchTest's
  simple fluent API) rather than hand-rolled reflection. They assert the assembly
  boundaries (core `DialogueDown` must not depend on `DialogueDown.Cli`, the
  `DialogueDown.Visualization*` projects, or any Spectre/Godot/console package) and
  the core's internal layering (the Dialogue AST stays decoupled from Markdown; the
  pipeline never calls back into the compilation orchestrator). Extend that suite
  when you add a layer or boundary.

## Before pushing

For fast compile feedback during an edit, after restoring once:

```bash
dotnet build DialogueDown.sln --configuration Release --no-restore \
  -p:RunAnalyzers=false
```

This is an inner-loop build only. It deliberately skips StyleCop and Sonar; run
the normal analyzer-enabled build and tests before pushing:

Use the VS Code tasks `test: project` and `test: filter` after a successful build
to run only the affected test project or behavior. `dotnet watch test` was
measured slower than a direct project run on the development machine, so it is
not part of the recommended loop.

```bash
dotnet test DialogueDown.sln --configuration Release --no-build -m:3
```

Run source-focused coverage when you change tested behavior (see
[`CONTRIBUTING.md`](../../CONTRIBUTING.md)).

# AGENTS.md

Guidance for coding agents working in DialogueDown. This mirrors
[`.github/copilot-instructions.md`](.github/copilot-instructions.md); both point at
`CONTRIBUTING.md` and `docs/contributing/` as the deep source of truth.

## What this is

Engine-agnostic, C#-first **dialogue compiler** library: it lowers a Markdown
dialogue script through distinct stages — **source → Markdown AST → Dialogue AST
→ desugared AST → (semantic analysis → graph → runtime, in progress)** — keeping
the core free of any Godot dependency so it stays reusable and unit-testable. An
optional TypeScript visualization renders each compiler stage as an interactive
report.

## Build and test

Uses the .NET 10 SDK (`global.json` pins 10.0.0 with `rollForward: latestMajor`). The
shipped libraries still multi-target `net8.0` and `net10.0` for Godot consumers, so CI
installs both runtimes; the SDK floor is not the consumer floor.
A plain `dotnet build` needs no Node — the built web report is committed.

```bash
# .NET library, CLI, and tests
dotnet restore DialogueDown.sln
dotnet format DialogueDown.sln --verify-no-changes --no-restore
dotnet build DialogueDown.sln --configuration Release --no-restore
dotnet test DialogueDown.sln --configuration Release --no-build --minimum-expected-tests 3000

# Source-focused coverage (CI fails below 90% line coverage, warns below 100%)
dotnet tool restore
dotnet test DialogueDown.sln --coverlet --coverlet-output-format cobertura --coverlet-include "[DialogueDown*]*" --minimum-expected-tests 3000

# Visualization client — only needed when changing web/ sources
cd src/DialogueDown.Visualization/web && npm ci && npm run check && npm run build
# Live integration — builds the CLI once, then launches the built DLL per server
cd src/DialogueDown.Visualization/web && npm run e2e:live
```

`dotnet format --verify-no-changes` is the code-style gate, and CI runs it
before the build. Run it too: code-style rules (`IDE####`, such as naming) do
**not** run during `dotnet build`, so a green build alone does not mean a green
CI. Note also that analyzer warnings are emitted only when a project actually
recompiles — repeating `dotnet build` with no changes prints zero warnings even
when violations exist. `dotnet format` always analyzes; the `build: verify` task
adds `--no-incremental` when a build's warning count has to be trusted.

`--minimum-expected-tests 3000` is not decoration: the Microsoft Testing Platform
forwards an argument it does not recognize to the test app, which then exits
without running anything and reports **"Zero tests ran"** with exit code 5 — output
that reads like a normal run. Keep the floor on any full-suite command you invent,
and treat a shortfall (exit code 9) as a broken command rather than a broken
suite. A framework-specific test-module glob fails the same way: two test projects
multi-target `net8.0;net10.0`, so six projects produce **eight** modules and a
`net10.0`-only glob silently runs 1,956 of 3,371 tests.

## Conventions

For inner-loop compile feedback, the non-default VS Code task `build: fast`
builds Release with `RunAnalyzers=false` after restore. It never replaces the
normal analyzer-enabled `build` and `test` tasks before pushing. After a build,
`test: project`, `test: filter`, and `test: class` run only the affected test
scope, stopping at the first failure. Frontend
inner-loop tasks similarly select one Vitest or Playwright scope; full
verification remains required before pushing.

- **Commits:** [Conventional Commits](https://www.conventionalcommits.org/); one
  logical change each; mark breaking changes with `BREAKING CHANGE:` in the footer.
- **Tests:** test-driven — a failing test first, then the code, then refactor. C#
  uses xUnit + NSubstitute; the web client uses Vitest + Playwright.
- **Design first for non-trivial work:** design notes live in
  [`docs/contributing/design-notes/`](docs/contributing/design-notes/README.md).
- **American English** in code, comments, docs, and commit messages.
- **SOLID and composition over inheritance;** write self-documenting code and
  reserve comments for public-API docs or a genuine *why* — a comment explaining
  *what* tangled code does is a signal to refactor it.
- **Keep the core engine-agnostic:** no Godot or rendering dependency in
  `DialogueDown`.
- When you change `web/src`, rebuild and commit `web/dist/report.html`.

## Engineering principles

Decision heuristics for changes here:

- **Readable first.** Follow the spirit of the Zen of Python — simple over
  complex, explicit over implicit, readable over clever. When readability and
  performance conflict, choose readability unless performance is genuinely
  critical (it rarely is); avoid premature optimization.
- **Model the domain (DDD).** Share one coherent vocabulary across code, tests,
  and docs — naming is a design decision, not an afterthought.
- **Test-driven and pyramid-shaped.** Mostly fast unit tests, fewer integration
  tests, minimal end-to-end; strive for 100% meaningful coverage. Treat tests as
  code and refactor them too.
- **SOLID, patterns applied judiciously.** Use a pattern only where it removes
  real duplication or coupling. Add a seam or interface where behavior is likely
  to change — but avoid premature generalization and over-abstraction (YAGNI).
- **Law of Demeter — tell, don't ask.** Send messages to your own collaborators,
  not to objects reached through them: prefer `owner.Send()` over
  `owner.Collaborator.Stranger.Send()`, which couples the caller to two shapes at
  once. This governs **objects with behavior, not data** — reading across the
  immutable AST records (`node.Span.Start`) and fluent chains
  (`parser.Or(x).Or(y)`) are deliberately exempt and must not be "fixed" into
  pass-through wrappers.
- **Enforce architecture with tests.** Adopt a fitting architectural pattern — here
  a dependency-light, engine-agnostic core with engine- and UI-specific code at the
  edges — and guard its boundaries with **architecture tests** that assert
  dependency direction (for example, `DialogueDown` must not depend on the CLI, the
  visualization projects, or any Godot/rendering package). Prefer an established
  library over hand-rolled reflection checks.
- **Prefer established solutions over hand-rolling,** especially outside the core
  library; keep the core dependency-light.
- **Refactor relentlessly** while the tests stay green, and **respect the
  conventions already in this repo.**

## Adding a design note

1. Create `docs/contributing/design-notes/<Note Name>.md` with a status callout.
   Use `> [!NOTE]` for the neutral status line (e.g. "Status: **implemented**") —
   status is informational, not an alarm. Reserve `> [!IMPORTANT]`/`> [!WARNING]`
   for genuine caveats or hazards.
2. Add the note to the **reading guide** in
   `docs/contributing/design-notes/README.md`: place it in the right area
   (Core, CLI, Visualization, or Other notes) and reading order, and keep that
   section's Mermaid reading-order chart current.
3. Register it in `docs/contributing/design-notes/toc.yml`.
4. Build the site: `dotnet tool run docfx docs/docfx.json` (add `--serve` to preview).

## Path-specific rules

GitHub Copilot applies these automatically by path; other agents should read the
one matching the files they touch:

- [`.github/instructions/csharp.instructions.md`](.github/instructions/csharp.instructions.md) — C# library, CLI, tests.
- [`.github/instructions/web.instructions.md`](.github/instructions/web.instructions.md) — the `web/` client.
- [`.github/instructions/docs.instructions.md`](.github/instructions/docs.instructions.md) — the `docs/` tree.

## Source of truth

- **[`CONTRIBUTING.md`](CONTRIBUTING.md)** — full setup, coverage, frontend, PR checklist.
- **[`docs/contributing/`](docs/contributing/index.md)** — architecture and design notes.
- **[`README.md`](README.md)** — overview and repository layout.

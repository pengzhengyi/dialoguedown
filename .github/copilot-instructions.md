# DialogueDown — Copilot instructions

Engine-agnostic, C#-first **dialogue compiler** library: it lowers a Markdown
dialogue script through distinct stages — **source → Markdown AST → Dialogue AST
→ desugared AST → (semantic analysis → graph → runtime, in progress)** — keeping
the core free of any Godot dependency so it stays reusable and unit-testable. An
optional TypeScript visualization renders each compiler stage as an interactive
report.

Path-specific rules live alongside this file and apply automatically:

- [`.github/instructions/csharp.instructions.md`](instructions/csharp.instructions.md) — C# library, CLI, and tests.
- [`.github/instructions/web.instructions.md`](instructions/web.instructions.md) — the `web/` visualization client.
- [`.github/instructions/docs.instructions.md`](instructions/docs.instructions.md) — the `docs/` tree and design notes.

## Build and test

Uses the .NET 8 SDK (`global.json` pins 8.0.0 with `rollForward: latestMajor`).
A plain `dotnet build` needs no Node — the built web report is committed.

```bash
# .NET library, CLI, and tests
dotnet restore DialogueDown.sln
dotnet build DialogueDown.sln --configuration Release --no-restore
dotnet test DialogueDown.sln --configuration Release --no-build -m:3

# Source-focused coverage (CI fails below 90% line coverage, warns below 100%)
dotnet tool restore
dotnet test DialogueDown.sln --settings coverage.runsettings --collect:"XPlat Code Coverage"

# Visualization client — only needed when changing web/ sources
cd src/DialogueDown.Visualization/web && npm ci && npm run check && npm run build
# Live integration — builds the CLI once, then launches the built DLL per server
cd src/DialogueDown.Visualization/web && npm run e2e:live
```

VS Code tasks (**Terminal → Run Task**) mirror these: `build`, `test`,
`web: check`, `verify: all`, and more.

Use `build: fast` only for inner-loop compile feedback after restore; it skips
analyzers. Use `test: project` or `test: filter` after a build for targeted
feedback. Frontend inner-loop tasks select one Vitest file or Playwright
file/title. The normal `build`/`test` and full frontend tasks remain the required
gate.

## Conventions

- **Commits:** [Conventional Commits](https://www.conventionalcommits.org/); one
  logical change each; mark breaking changes with `BREAKING CHANGE:` in the footer.
- **Tests:** test-driven — write a failing test first, then the code, then refactor.
- **Design first for non-trivial work:** design notes live in
  [`docs/contributing/design-notes/`](../docs/contributing/design-notes/); read
  them in pipeline order to understand the compiler.
- **American English** in code, comments, docs, and commit messages.
- **SOLID and composition over inheritance;** write self-documenting code and
  reserve comments for public-API docs or a genuine *why* — a comment explaining
  *what* tangled code does is a signal to refactor it.
- **Keep the core engine-agnostic:** no Godot or rendering dependency in
  `DialogueDown`.

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

## Source of truth

Point at these rather than restating them, so instructions never drift from the
docs:

- **[`CONTRIBUTING.md`](../CONTRIBUTING.md)** — full development setup, coverage,
  frontend, and the pull-request checklist.
- **[`docs/contributing/`](../docs/contributing/index.md)** — architecture, the
  compiler pipeline, and design notes.
- **[`README.md`](../README.md)** — project overview and repository layout.

`AGENTS.md` at the repository root mirrors this file for other agent tools.

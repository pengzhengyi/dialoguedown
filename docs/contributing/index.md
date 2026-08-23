# Contributing

Developer-facing documentation for **working on DialogueDown** itself — its
architecture, the reasoning behind each compiler stage, and how to get set up.

## Start here

- **[Contribution guide](https://github.com/pengzhengyi/dialoguedown/blob/main/CONTRIBUTING.md)**
  — how to report issues, develop, test, and open pull requests.
- **[Code of conduct](https://github.com/pengzhengyi/dialoguedown/blob/main/CODE_OF_CONDUCT.md)**
  and **[security policy](https://github.com/pengzhengyi/dialoguedown/blob/main/SECURITY.md)**.
- **[How this project is tested](testing.md)** — the kinds of test here, what each
  one is for, and which to write. Worth reading before your first test.

## Understand the design

- **[Design notes](design-notes/README.md)** — one note per component and compiler
  stage (the Markdown front-end, the transpiler, desugaring, the visualization,
  the CLI, and more), each recording the goal, key decisions, and tradeoffs.
- **[API reference](../api/index.md)** — the generated C# API, useful when reading
  or extending the library.

## The compiler pipeline

DialogueDown lowers a script through distinct stages, each with its own design
note under [Design notes](design-notes/README.md):

**source → Markdown AST → Dialogue AST → desugared AST → (semantic analysis →
graph → runtime, in progress)**.

Reading the notes in that order is the fastest way to learn how the compiler fits
together before making a change.

## Enforced architecture boundaries

The pipeline's shape is not just convention — it is conditional by architecture tests
in
[`tests/DialogueDown.Architecture.Tests`](https://github.com/pengzhengyi/dialoguedown/tree/main/tests/DialogueDown.Architecture.Tests)
(built on NetArchTest.eNhancedEdition). They fail the build if a change breaks the
intended dependency direction:

- The engine-agnostic core (`DialogueDown`) must not depend on the CLI, the
  visualization projects, or any Spectre/Godot/console package.
- Each visualization layer depends only downward
  (`Visualization.Live → Visualization → core`).
- Inside the core, the Dialogue AST stays decoupled from Markdown, and no pipeline
  stage calls back into the compilation orchestrator.

When you add a layer or boundary, extend that suite so the rule travels with the
code.

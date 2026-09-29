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

A script is lowered through distinct stages — parse, transpile, desugar, validate,
analyze, build the graph, then write a playbook a runtime plays. The
[pipeline diagram](design-notes/README.md#core-the-compiler-pipeline) links each
stage to its design note; reading the notes in that order is the fastest way to
learn how the compiler fits together.

## Enforced architecture boundaries

The dependency direction between projects and between core layers is enforced by
architecture tests that fail the build when a change breaks it. The rules, and how
to extend them, are in [How this project is tested](testing.md#architecture-tests).

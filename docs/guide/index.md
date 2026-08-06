# Guide

Writer-facing documentation for **authoring dialogue** with DialogueDown. If you
want to *write* branching dialogue — not modify the compiler — start here.

## Start here

- **[Your first script](first-script.md)** — a short walkthrough: say a line, add
  a speaker, branch on a choice, jump between scenes, and talk to your game.

## In this section

- **[Overview](overview.md)** — the architecture at a glance, the three
  representations a script passes through (source → compiled model → runtime
  graph), and the current implementation status.
- **[Command line](cli.md)** — installing the `ddown` CLI, compiling a script,
  and opening the interactive visualization.
- **[Script language specification](script-language.md)** — the complete
  writer-facing syntax, split across three reference pages:
  - **[Speakers and lines](speakers-and-lines.md)** — who is speaking, styling,
    tags, and images.
  - **[Game state](game-state.md)** — queries that read from your game and
    commands that tell it to act.
  - **[Structure and flow](structure-and-flow.md)** — scenes, succession,
    choices, jumps, conditions, and ending a run.
- **[Project configuration](configuration.md)** — the `dialogue.toml` that
  configures your project's speakers, the default speaker, and the compilation
  mode, and how the CLI finds it.
- **[Error codes](error-codes.md)** — every diagnostic the compiler can report,
  with what causes it and how to fix it.

> [!NOTE]
> DialogueDown is in early development. The language described here is
> implemented, but the compiler model and runtime behavior may still change as
> the library matures.

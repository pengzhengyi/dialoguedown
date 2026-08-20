# AGENTS.md

Guidance for coding agents working in DialogueDown.

This file carries what an agent needs before it can act: what the project is and
how to build and test it. Everything else — conventions, engineering principles,
and how to add a design note — lives in
[`.github/copilot-instructions.md`](.github/copilot-instructions.md), which is the
canonical instruction file. Read it next; this file deliberately does not repeat
it, so the two cannot drift apart.

## What this is

An engine-agnostic, C#-first **dialogue compiler** that lowers a Markdown script
through distinct stages, keeping the core free of any Godot dependency. An optional
TypeScript visualization renders each stage as an interactive report. The pipeline
and its stages are described in
[`.github/copilot-instructions.md`](.github/copilot-instructions.md) and
[`docs/contributing/`](docs/contributing/index.md).

## Build and test

Uses the .NET 10 SDK (`global.json` pins the floor). A plain `dotnet build` needs
no Node — the built web report is committed.

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

> [!IMPORTANT]
> Two flags above are load-bearing, not decoration. `dotnet format
> --verify-no-changes` is a separate gate because code-style rules do **not** run
> during `dotnet build`, so a green build is not a green CI. And
> `--minimum-expected-tests 3000` catches the Microsoft Testing Platform's silent
> failure mode, where an unrecognized argument makes the run report **"Zero tests
> ran"** with exit code 5 — output that reads like success.
> [`.github/copilot-instructions.md`](.github/copilot-instructions.md) explains both
> in full, along with the VS Code tasks that mirror these commands.

## Read next

| Document | What it covers |
| --- | --- |
| [`.github/copilot-instructions.md`](.github/copilot-instructions.md) | Conventions, engineering principles, and the path-specific rules below. |
| [`CONTRIBUTING.md`](CONTRIBUTING.md) | Full setup, coverage, frontend, and the pull-request checklist. |
| [`docs/contributing/`](docs/contributing/index.md) | Architecture, the compiler pipeline, and the design notes. |
| [`README.md`](README.md) | Project overview and repository layout. |

Path-specific rules apply automatically in GitHub Copilot; other agents should
read whichever matches the files they touch:

- [`.github/instructions/csharp.instructions.md`](.github/instructions/csharp.instructions.md) — C# library, CLI, tests.
- [`.github/instructions/web.instructions.md`](.github/instructions/web.instructions.md) — the `web/` client.
- [`.github/instructions/docs.instructions.md`](.github/instructions/docs.instructions.md) — the `docs/` tree, including how to add a design note.

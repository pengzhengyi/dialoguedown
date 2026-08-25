# Dialogue AST Visualization Tab

> [!NOTE]
> Status: **implemented** (an enhancement to
> [Compilation Visualization](./Compilation%20Visualization.md)). The transpiler's
> Dialogue AST has landed on `main`, so the report can now show it as a **second
> graph tab** beside the Markdown AST — one more projection over the existing walk,
> model, and renderers, with no bespoke graph code.
>
> Like the rest of the visualization tooling, this surface is "vibe-coded" (see the
> visualization note's maturity caveat); the core engine stays the reviewed surface.

## Table of contents

- [Goal](#goal)
- [Ubiquitous language](#ubiquitous-language)
- [Design](#design)
- [Node mapping](#node-mapping)
- [Key design decisions](#key-design-decisions)
- [Testability](#testability)

## Goal

The report already renders the Markdown AST as an interactive graph tab. The
transpiler now turns that Markdown AST into the **Dialogue AST** — lines, speakers,
choices, game calls, tags — so the report should show that stage too, as its own
tab, letting a reader see how the source becomes dialogue.

Adding a stage is one small projection: `CompilationVisualizer.BuildStages`
transpiles the parsed document and projects the result the same way it projects the
Markdown AST. The walk, display model, renderers, and the whole front-end are
unchanged — the new tab appears because there is a new stage in the payload.

In scope:

- A `DialogueAstProjection` that labels every Dialogue AST node, yields its
  children, and slices the source each node came from.
- A production composition root for the transpiler, so the visualizer can obtain a
  ready `IScriptTranspiler` (the CLI `compile` path is still a stub).
- The Dialogue AST appended as the second stage in `BuildStages`.

Out of scope:

- Any change to the graph walk, display model, renderers, or front-end.
- The `compile` CLI pipeline (still a separate, pending concern).

## Ubiquitous language

Reuses the [Compilation Visualization](./Compilation%20Visualization.md) language
(**stage**, **projection**, **stage description**, **category**). New here:

| Term | Meaning |
| --- | --- |
| **Dialogue AST** | The transpiler's output IR (`ScriptDocument` and its `ScriptNode`s: lines, speakers, choices, game calls, tags). |
| **Transpiler composition root** | The single place that wires the transpiler's builder graph, so a caller gets a ready `IScriptTranspiler` without knowing the wiring. |

## Design

```text
source ──► IMarkdownParser.Parse ──► MarkdownDocument ──► MarkdownAstProjection ─┐
                                          │                                       ├─► stages[]
                                          └─► IScriptTranspiler.Transpile ──► ScriptDocument ──► DialogueAstProjection ─┘
```

- **`ScriptTranspilerFactory.CreateDefault()`** (new, `DialogueDown.Script.Transpiler`)
  wires the builder graph (block, line, inline, speaker, game-call, tag) with their
  standard parsers and returns an `IScriptTranspiler`. The transpiler and builders
  are `internal`; the visualization project already sees them through
  `InternalsVisibleTo`. The test `TranspilerBuilderFactory` keeps its granular
  builder accessors for builder-level tests.
- **`DialogueAstProjection : INodeProjection<object>`** (new,
  `DialogueDown.Visualization`) mirrors `MarkdownAstProjection`: it matches each node
  by runtime type (the AST families share no single base usable as `TNode`), returns
  a `NodeDescription` (label, attributes, span-sliced source, category), and yields
  each node's children from `Neighbors`. `ScriptDocument` is the root (a plain
  container, like `MarkdownDocument`).
- **`CompilationVisualizer.BuildStages`** parses, transpiles via the factory, and
  returns `[markdownStage, dialogueStage]`.

## Node mapping

Nodes reuse the cross-stage color **categories** so corresponding concepts share a
color across stages (a Markdown code span and the `call` it becomes are both red).

| Dialogue AST node | Label | Category |
| --- | --- | --- |
| `ScriptDocument` | Document | document |
| `SceneHeading` | Scene heading (H*n*) | structure |
| `Line` | Line | speech |
| `SpeakerDeclaration` / `PartialSpeakerDeclaration` / `SpeakerNameReference` / `SpeakerIdReference` | Speaker (…) | speech |
| `Choices` / `Choice` | Choices / Choice | choice |
| `Text` | Text | text |
| `StyledText` | Styled text (*style*) | styling |
| `Link` | Link | jump |
| `JumpIndicator` | Jump indicator | jump |
| `Image` | Image | media |
| `DefaultCommand` / `CustomCommand` / `Query` | Command / Query | call |
| `LineBreak` | Line break | break |
| `ReservedTag` / `CustomTag` | Tag (…) | **tag** (new) |

## Key design decisions

### D1 — One more projection, nothing else

The Dialogue AST tab is a `DialogueAstProjection` plus one line in `BuildStages`.
The generic walk, `DisplayGraph`, JSON, renderers, and the whole front-end (tabs,
categories, tooltips) already handle "another stage", so no other code changes.

### D2 — A production transpiler composition root

The transpiler's builder graph is only wired in a **test** factory today; the CLI
`compile` command is still a stub. The visualizer needs a real transpiler, so a
small `ScriptTranspilerFactory.CreateDefault()` becomes the one production place
that wires it — reusable by the `compile` pipeline when it lands.

### D3 — Tags get a new category; everything else reuses existing ones

The Dialogue AST is the first stage with **tags**, which have no Markdown
counterpart, so they get a new `tag` category and color. Every other node maps to
an existing category (see the [mapping](#node-mapping)) so colors stay continuous
across the two stages.

## Testability

- **Transpiler factory** (unit, .NET): `CreateDefault()` transpiles a simple script
  to the expected `ScriptDocument` (a smoke test that the graph is wired correctly).
- **Projection** (unit, .NET): labels, categories, span-sliced source, and children
  for representative nodes (line with speaker, choices, game call, tag, styled text);
  an unsupported node throws.
- **Build stages** (unit, .NET): `BuildStages` returns two stages titled
  `Markdown AST` and `Dialogue AST`, each with a non-empty description.
- **Report** (e2e, Playwright): a real report now shows three tabs (Source, Markdown
  AST, Dialogue AST); the launcher/live specs assert the third tab.

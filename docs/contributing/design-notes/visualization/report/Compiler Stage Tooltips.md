# Compiler Stage Tooltips

> [!NOTE]
> Status: **implemented** (an enhancement to
> [Compilation Visualization](./Compilation%20Visualization.md)). Each compiler
> stage tab gains a hover tooltip that says what that stage's graph shows, and the
> report's generic subtitle is slimmed to a short hint. The text belongs to the
> stage — authored on its projection — so every stage, including future ones,
> carries its own tip.
>
> Like the rest of the visualization tooling, this surface is "vibe-coded" (see the
> visualization note's maturity caveat); the core engine stays the reviewed surface.

## Table of contents

- [Goal](#goal)
- [Ubiquitous language](#ubiquitous-language)
- [Design](#design)
- [Key design decisions](#key-design-decisions)
- [Testability](#testability)

## Goal

The report opens on the Source tab and shows one tab per compiler stage, each an
interactive graph. A single static subtitle — "the Source tab shows the document
with a live preview; each compiler stage appears as an interactive graph in its own
tab" — tries to explain all of them at once, and says nothing about what any
*individual* stage represents.

Move that explanation onto the tabs: hovering a stage tab shows a tip describing
what that stage's graph is (for example, hovering **Markdown AST** explains it is
the Markdown tree the front end parses from the source). The subtitle shrinks to a
short hint that invites the hover. Each tip is a property of the stage, so the set
of tips stays correct as stages are added.

In scope:

- A one-line **description** on every compiler stage, authored where the stage is.
- A hover tooltip on each stage tab, and on the Source tab, carrying that text.
- Slimming the static subtitle to a short discovery hint.

Out of scope:

- Long-form or multi-paragraph help (the footer "How to use" already covers usage).
- Any change to the graphs, node tooltips, or the stage set itself.

## Ubiquitous language

| Term | Meaning |
| --- | --- |
| **Stage description** | A short, one-line explanation of what a compiler stage's graph shows, authored on the stage's projection and shown as the tab's tooltip. |
| **Stage title** | (Existing.) The stage's short tab label, e.g. `Markdown AST`. |
| **Projection** | (Existing.) `INodeProjection<TNode>` — the seam that names a stage and walks its IR. Now also owns the stage description. |

## Design

A stage description travels the same path the title already does, so no new wiring
is introduced — only one more field.

```text
INodeProjection.Description  ──►  GraphWalk  ──►  DisplayGraph.Description
        (authored per stage)                          │
                                                       ▼  (camelCase JSON, automatic)
                                          Stage.description (client model)
                                                       │
                                                       ▼
                                   app.ts attaches a Tippy tooltip to the tab
```

- **The projection owns the description.** `INodeProjection<TNode>` gains a
  `Description` next to `Title`; each projection authors its own one-liner
  (`MarkdownAstProjection` describes the Markdown syntax tree). `GraphWalk` copies
  it into `DisplayGraph`, which serializes its public properties to camelCase JSON,
  so the client `Stage` gains `description` with no serializer change.
- **The tab carries the tip.** `app.ts` attaches a hover tooltip to each stage tab
  from `stage.description`, reusing the report's existing **Tippy.js** dependency
  (already used for graph-node tooltips) so the look and accessibility match.
- **The Source tab is special.** The Source tab is the *input*, not a projected
  stage, so its tip is a small frontend constant rather than a model field.
- **The subtitle shrinks.** The `<hgroup>` subtitle becomes a short hint (for
  example, "Hover a tab to see what each stage shows.") so the per-tab tips are
  discoverable.

## Key design decisions

### D1 — The description lives on the stage, not in a frontend map

A description authored next to each stage's title keeps one source of truth and
means every new stage (the Dialogue AST and desugaring stages to come) ships its own
tip automatically. A frontend title→text map would silently miss new stages and
couple the copy to title strings, so it is rejected.

### D2 — Reuse Tippy for the tab tooltips

The report already depends on Tippy.js for rich, accessible node tooltips. The tab
tips reuse it, so they inherit the same styling, delay, and keyboard/`aria`
behavior, and add no new dependency. A bare `title` attribute would be
inconsistent and less accessible.

### D3 — The Source tab tip is a frontend constant

The Source tab shows the document and its live preview — the compiler input, not a
projected IR — so it has no projection to own a description. Its tip is a short
constant in the client, kept beside where the Source tab is built.

## Testability

- **Projection** (unit, .NET): `MarkdownAstProjection.Description` is a non-empty
  one-liner; the walk copies it onto `DisplayGraph`.
- **Serialization** (unit, .NET): a serialized report carries each stage's
  `description` field.
- **Tabs** (Playwright e2e): hovering a stage tab shows a Tippy tooltip with its
  description, and hovering the Source tab shows the constant tip. The tab wiring
  lives in the browser-integration layer (`app.ts`), covered end-to-end rather
  than by jsdom unit tests.

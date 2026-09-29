# Semantic Analyzer

> [!NOTE]
> Status: **implemented**. The resolve-and-validate
> [pipeline stage](../README.md#core-the-compiler-pipeline): it reads the desugared
> tree and produces a `SemanticModel` — a unified speaker table, a nested scene tree
> with an anchor table, and resolved jumps — which the
> [dialogue graph](./Dialogue%20Graph.md) builder lowers into a flow.

## Table of contents

- [Ubiquitous language](#ubiquitous-language)
- [The analyzer at a glance](#the-analyzer-at-a-glance)
- [Interfaces and abstractions](#interfaces-and-abstractions)
- [Key design decisions](#key-design-decisions)
- [Error and boundary cases](#error-and-boundary-cases)
- [Testability](#testability)

## Ubiquitous language

| Term | Meaning |
| --- | --- |
| **Semantic model** | The analyzed artifact: the desugared tree plus its tables and resolutions, bound to that tree like Roslyn's `SemanticModel`. |
| **Speaker symbol** | A resolved speaker identity (`Name?`, `Id?`, merged tags, `IsDefault`), distinct from the AST `Speaker` prefix. |
| **Speaker table** | The lookup from a name **or** an `@id` to a speaker symbol. |
| **Scene** | A heading, its anchor, its child scenes, and the blocks it owns; distinct from the AST `SceneHeading`. |
| **Anchor** | A scene's GitHub-style slug (`## Play tennis` → `play-tennis`), the target a jump resolves against. |
| **Jump resolution** | `SceneJump`, `TerminalJump` (`#END`), `FileScopedJump` (a target naming a file), or `UnresolvedJump`. |
| **Tree index** | `DialogueTreeIndex`: every node indexed once by type, so a sub-pass asks `OfType<T>()` instead of walking. |
| **Sub-pass** | One isolated analysis step with explicit inputs and outputs. |

## The analyzer at a glance

```text
Analyze(desugared, context):
    index            = DialogueTreeIndex.Build(desugared)

    # build tables
    (scenes, anchors) = SceneBuilder.Build(desugared, sink)
    speakers          = SpeakerBinder.Bind(configuredSpeakers, index.OfType<Speaker>(), sink)

    # resolve and validate against them
    jumps             = JumpResolver.Resolve(index.OfType<Jump>(), anchors, sink)
    TagValidator.Validate(index.OfType<ReservedTag>(), sink)
    ChoiceLabelValidator.Validate(index.OfType<Choice>(), sink)

    return SemanticModel(desugared, speakers, scenes, anchors, jumps)
```

`configuredSpeakers` come from `CompilerOptions` through
`ConfiguredSpeakerBuilder`, which turns each into a `SpeakerDeclaration` (see
[Configuration](../configuration/Configuration.md)). Every sub-pass reports into
the compilation's sink and recovers; the codes and recoveries are listed in
[Diagnostics and Validation](../diagnostics/Diagnostics%20and%20Validation.md#recovery-at-each-reporting-site).

## Interfaces and abstractions

| Type | Responsibility |
| --- | --- |
| `ISemanticAnalyzer` / `SemanticAnalyzer` | `SemanticModel Analyze(DesugaredScriptDocument, DiagnosticsContext)`; constructed from `ISemanticAnalyzerOptions`. |
| `SemanticModel` | The tree, `SpeakerTable`, scene root, `AnchorTable`, and `JumpResolutionTable`. |
| `SceneBuilder`, `Scene`, `AnchorTable`, `Slug` | Headings → scene tree → slug lookup. |
| `SpeakerBinder`, `SpeakerSymbol`, `SpeakerTable` | AST and configured speakers → one table; `Resolve(Speaker)`. |
| `JumpTarget`, `JumpResolver`, `JumpResolution`, `JumpResolutionTable` | A jump's target → its resolution, keyed by the `Jump` node. |
| `TagValidator` | Reserved tags against `ReservedTagNames` (`default`). |
| `ChoiceLabelValidator` | A player option with nothing a menu could show reports `DLG2017`. |

`DialogueTreeIndex` lives in `Script.Desugar`, beneath both validation and
analysis, since the [structural validator](../diagnostics/Diagnostics%20and%20Validation.md#the-structural-validator)
shares it.

## Key design decisions

### D1 — A separate semantic model, not an enriched tree

The AST stays immutable and untouched. Jump resolutions live in a side table keyed
by `Jump` node reference; speaker resolution is a method, `SpeakerTable.Resolve`, so
the rules live in one place. The model holds the tree because its tables only
annotate it, and the graph builder needs both. Taking a `SemanticModel` makes
analysis impossible to skip, just as taking a `DesugaredScriptDocument` makes
desugar impossible to skip.

### D2 — Pure sub-passes in two layers

Build the tables (scenes, speakers), then resolve and validate against them. Each
sub-pass is a function of explicit inputs, so it is tested with no global state.

### D3 — One index, built once

`DialogueTreeIndex` walks the tree once and indexes each node under every type in
its inheritance chain, in document order, so `OfType<Speaker>()` works and results
stay ordered. It is rebuilt per compile; it caches no subtrees and survives no edits.

### D4 — One speaker table with dual keys and a name invariant

`Alice @A` means `Alice:` and `@A:` are the same speaker, so the table maps both
keys to one `SpeakerSymbol`. One document-order pass binds:

| AST node | Action |
| --- | --- |
| `SpeakerDeclaration` | create or enrich the symbol; register its name and id |
| `SpeakerNameReference` (`Alice:`) | resolve by name; auto-declare on first use |
| `SpeakerIdReference` (`@A:`) | resolve by id; auto-declare on first use |
| `PartialSpeakerDeclaration` (`@A #x:`) | resolve by id; merge the tags |
| `##default` on a declaration | mark the default; at most one |

Configured speakers bind first, in the same maps, so a configured name used in the
script is the same speaker.

Auto-declaration is permissive, and a closing **name invariant** makes it safe:
every symbol must end with a name. An `@A:` before its `Alice @A:` declaration ends
named and is valid; a misremembered `@a:` never gets a name and reports `DLG2007`.
A speaker's identity is its name; an `@id` is secondary.

The default is chosen by precedence: an in-file `##default`, else the configured
default, else an **anonymous default** — a nameless, non-referable symbol the binder
mints so a narration line like "The wind blows" still resolves. `Resolve` therefore
always returns a symbol.

### D5 — Scenes nest by the outline rule

A heading nests under the nearest shallower one and owns the blocks up to the next
heading of the same or shallower level. Blocks before the first heading belong to an
implicit root scene. Start and End sentinels are flow concerns and belong to the
graph builder.

### D6 — GitHub-style slugs, but collisions are errors

`Slug` ports github-slugger's rules exactly, so a target a writer copies from a
Markdown preview matches the compiler's anchor character for character. Two scenes
with the same slug report `DLG2001` instead of getting a `-1` suffix: an anchor is a
jump target, and silent disambiguation would make `=> [X](#scene)` resolve
arbitrarily. A heading that slugs to empty (`## !!!`) reports `DLG2002`.

### D7 — Jumps resolve to scenes, not edges

`JumpTarget` splits a target at the first `#`. A target with no file part is looked
checked for the reserved `#END` (`TerminalJump`), then looked up in the anchor table
(`SceneJump`, or `DLG2009` and `UnresolvedJump`). A target
that names a file is a `FileScopedJump` with a `DLG2016` warning — cross-file
resolution is the [Cross-File Jump Resolution](../language/Cross-File%20Jump%20Resolution.md)
note. Edges are the graph builder's job.

### D8 — Reserved tags come from a known set

`##name` must be in `ReservedTagNames` (`default`); an unknown one reports `DLG2008`
and is left inert. Custom tags pass through. A tag with nothing to attach to is the
transpiler's concern (`DLG1101`).

## Error and boundary cases

| Case | Behavior |
| --- | --- |
| Empty document | An empty model with only the root scene. |
| No headings | The whole document is the root scene; local-anchor jumps report `DLG2009`. |
| Speaker conflicts | `DLG2003`–`DLG2006`; the first binding is kept. |
| An `@id` never named | `DLG2007`; kept as an unnamed placeholder. |
| Duplicate slug / empty slug | `DLG2001` / `DLG2002`. |
| Jump to a missing anchor | `DLG2009`; `UnresolvedJump`. |
| Jump with an empty target | `UnresolvedJump`, with no diagnostic. |
| `=> [End](#END)` | `TerminalJump`. |
| Jump naming a file | `FileScopedJump`; `DLG2016` warning. |
| Unknown reserved tag | `DLG2008`. |
| `null` document or context | `ArgumentNullException`. |

## Testability

- Each sub-pass is tested alone on a hand-built desugared tree: nesting and slugs,
  dual-key binding and the name invariant, resolution against a given anchor table,
  the reserved-tag check.
- `DialogueTreeIndex` is tested for base-type queries and document order.
- The analyzer is tested through the real pipeline on multi-scene scripts written as
  raw string literals.

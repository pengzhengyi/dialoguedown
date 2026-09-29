# AST Stage Tabs

> [!NOTE]
> Status: **implemented**. The **Dialogue AST** and **Desugared AST** tabs draw the transpiler's
> tree and the desugarer's normalized tree through one projection, so the two read identically
> apart from the nodes desugaring adds.

## Ubiquitous language

Reuses the [Compilation Visualization](./Compilation%20Visualization.md) terms (**stage**,
**projection**, **category**) and the [Desugar](../../core/Desugar.md) terms.

| Term | Meaning |
| --- | --- |
| **Dialogue AST** | The transpiler's tree: `ScriptDocument` and its `ScriptNode`s. |
| **Desugared AST** | The same tree after desugaring (`DesugaredScriptDocument`): jumps assembled from an arrow and its link, and a `DefaultSpeaker` filled on lines that name none. |
| **Synthetic node** | A node a stage inserts, with no source text of its own, marked by a zero-width span (`SourceSpan.IsEmpty`). |

## Design

`CompilationVisualizer` compiles once through `IScriptCompiler` and projects `result.Script` and
`result.Desugared` with the same `DialogueAstProjection`, constructed with each tab's title and
description (`ScriptDisplayExtensions.ToDisplayGraph`).

### D1 — One projection for both tabs

The desugared tree is the Dialogue AST vocabulary plus two node kinds, so one projection,
parameterized by title and description, draws both. The tabs stay visually continuous by
construction, and `DefaultSpeaker` and `Jump` are ordinary Dialogue AST node types, not special
cases.

### D2 — Categories carry color across stages

Each node maps to a cross-stage category, so a concept keeps its color from the Markdown AST
onward: a code span and the command it becomes are both `call`.

| Node | Label | Category |
| --- | --- | --- |
| `ScriptDocument` | Document | `document` |
| `SceneHeading` | Scene heading (H*n*) | `structure` |
| `Line` | Line | `speech` |
| `SpeakerDeclaration`, `PartialSpeakerDeclaration`, `SpeakerNameReference`, `SpeakerIdReference`, `DefaultSpeaker` | Speaker (declaration / partial / by name / by id / default) | `speech` |
| `Choices`, `Choice`, `RandomChoices`, `RandomOption` | Choices (ordered / unordered), Choice, Random choices, Random option | `choice` |
| `ControlLine`, `ControlBlock`, `Branch` | Control line, Control block, Branch / Else branch | `control` |
| `Text` | Text | `text` |
| `StyledText` | Styled text (*style*) | `styling` |
| `Link`, `JumpIndicator`, `Jump` | Link, Jump indicator, Jump | `jump` |
| `Image` | Image | `media` |
| `DefaultCommand`, `CustomCommand`, `Query`, `Condition` | Command (default / *name*), Query, Condition | `call` |
| `LineBreak` | Line break | `break` |
| `ReservedTag`, `CustomTag` | Tag (reserved / custom) | `tag` |

A node type the projection does not know throws `ArgumentException`, so a new AST node fails
loudly rather than drawing a blank.

### D3 — A synthetic node shows "inserted", not empty source

A `DefaultSpeaker` has a zero-width span, and slicing it would draw an empty Source block. The
projection passes no source for an empty span, and the inspector shows *Inserted by the compiler —
no source of its own.* The `span` attribute (`[n, n)`) still says where it was inserted; see
[Node Inspector](../graph/Node%20Inspector.md#d4--a-synthetic-node-has-a-position-but-no-text).

### D4 — Named as a series

**Markdown AST → Dialogue AST → Desugared AST**: each tab is an *X AST*, and each stage
description says what that stage did.

## Error and boundary cases

| Case | Behavior |
| --- | --- |
| A compile that halts before desugaring | The Desugared AST tab is unavailable; see [Unavailable stages](./Compilation%20Visualization.md#unavailable-stages). |
| A dangling `=>` with no link | The desugarer keeps it as `Text`, so it draws as Text, not a Jump. |

## Testability

| Level | Covers |
| --- | --- |
| .NET — `DialogueAstProjection` | Labels, categories, span-sliced source, and children for each node family; `DefaultSpeaker` has no source and a zero-width span; `Jump` yields its label fragments; an unknown node throws. |
| .NET — `CompilationVisualizer` | Both tabs appear with their titles and descriptions; a stub `IScriptCompiler` is the source of the stages; a speaker-less line yields a `Speaker (default)` node. |
| Playwright | Selecting the synthetic default speaker shows the inserted note and no source block. |

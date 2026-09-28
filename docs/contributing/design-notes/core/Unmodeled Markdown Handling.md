# Unmodeled Markdown Handling

> [!NOTE]
> Status: **implemented**. How the
> [Markdown front end](./Markdown%20Front-End.md) treats a construct it does not
> model as dialogue — **keep** its text or **ignore** it — and how a project
> overrides the defaults in code or in `dialogue.toml`.

## Handling model

The front end models headings, paragraphs, lists, blockquotes, links, images, code
spans, emphasis, and line breaks. Everything else is *unmodeled*, and each unmodeled
kind resolves to one handling:

| Handling | Meaning |
| --- | --- |
| `Keep` | The construct's source text becomes dialogue text, sliced from its span. Its text is kept, not its structure. |
| `Ignore` | The construct is left out of the script, and the front end reports `DLG1114` (see [Ignored Markdown Diagnostic](../diagnostics/Ignored%20Markdown%20Diagnostic.md)). |

Comments and leading front matter are always discarded and are not part of the
policy.

## Kinds and defaults

The defaults ignore authoring aids and keep content whose intent is unclear:

| Kind (`UnmodeledNodeKind`) | TOML name | Example | Default | Why |
| --- | --- | --- | --- | --- |
| `CodeBlock` | `code-block` | a fenced ` ```mermaid ` block | `Ignore` | Diagrams and code illustrate; they are not dialogue. |
| `ThematicBreak` | `thematic-break` | `---` | `Ignore` | A visual divider, not words. |
| `Table` | `table` | `\| Speaker \| Mood \|` | `Ignore` | Reference data, not dialogue. |
| `LinkReferenceDefinition` | `link-reference-definition` | `[label]: target` | `Ignore` | CommonMark plumbing that no tool renders. |
| `RawHtml` | `raw-html` | `<div>`, `<br>` | `Keep` | The writer typed it deliberately. |
| `Autolink` | `autolink` | `<https://example.com>` | `Keep` | A URL that is content. |
| `Other` | `other` | anything else unmodeled | `Keep` | Kept rather than silently lost. |

A table is recognized only because the front end enables Markdig's pipe-table
extension; stray pipes that do not form a table stay literal text.

## The policy seam

```csharp
// DialogueDown.Configuration — the vocabulary a project configures with.
public enum UnmodeledNodeKind
{
    CodeBlock, ThematicBreak, Table, RawHtml, Autolink, LinkReferenceDefinition, Other,
}
public enum UnmodeledNodeHandling { Keep, Ignore }

// DialogueDown.Markdown — the seam the front end reads.
internal interface IUnmodeledNodeHandlingPolicy
{
    UnmodeledNodeHandling HandlingFor(UnmodeledNodeKind kind);
}
```

`DefaultUnmodeledNodeHandlingPolicy.Instance` implements the table above.
`UnmodeledNodeHandlingPolicies.For(overrides)` returns that singleton when there are
no overrides, and a `ConfiguredUnmodeledNodeHandlingPolicy` layering the overrides
over it otherwise. The composition roots build it from
`CompilerOptions.UnmodeledMarkdown`, and `MarkdigMarkdownParser` requires one.

The enums live in `DialogueDown.Configuration` because they are what a project
writes, and configuration must not depend on the front end; the policy stays in
`DialogueDown.Markdown`, so the dependency runs one way.

A policy can also be written in code:

```csharp
internal sealed class KeepTablesHandlingPolicy : IUnmodeledNodeHandlingPolicy
{
    public UnmodeledNodeHandling HandlingFor(UnmodeledNodeKind kind) => kind switch
    {
        UnmodeledNodeKind.Table => UnmodeledNodeHandling.Keep,
        _ => DefaultUnmodeledNodeHandlingPolicy.Instance.HandlingFor(kind),
    };
}
```

## Configuration

A project overrides handlings under `[markdown.unmodeled]`; omitted kinds keep the
defaults, so the section lists only exceptions:

```toml
# dialogue.toml
[markdown.unmodeled]
table      = "keep"
code-block = "ignore"
```

`UnmodeledMarkdownNames` holds the kebab-case names, shared by the loader and every
tool that displays the configuration. An unknown kind or handling is a located
error. The [Configuration Loader](../configuration/Configuration%20Loader.md) reads the
section; the user-facing schema is the
[configuration guide](../../../guide/configuration.md).

## Visualization provenance

The report shows each construct's fate without reimplementing the policy:

- an **ignored** construct produces `DLG1114`, and the semantic-token projection
  turns that range into `IgnoredMarkdown`, so Source and Preview show it as present in
  the file but absent from the script;
- a **kept** construct is ordinary dialogue text and renders as such;
- a project override changes whether `DLG1114` exists, so highlighting follows it.

The report does not record "kept because unmodeled" after flattening, and the
Config tab does not list the resolved handling for every kind. If a UI needs either,
`MarkdigUnmodeledNodeHandler` is the one site that knows the kind, handling, and span
together.

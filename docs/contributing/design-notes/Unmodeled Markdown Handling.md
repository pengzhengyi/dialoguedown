# Unmodeled Markdown handling

How the Markdown front-end treats Markdown constructs it does **not** model as
dialogue — each is either kept **as raw text** or **ignored** — and how to
override the defaults with a custom policy.

> [!NOTE]
> The in-code policy and defaults below are implemented and are the source of
> truth for *behavior*. The **configuration format** is TOML (a project
> `dialogue.toml`; see [Configuration format](#configuration-format)); the loader
> that reads it is future work.

## Table of contents

- [Background](#background)
- [Handling model](#handling-model)
- [Kinds and defaults](#kinds-and-defaults)
- [The policy seam](#the-policy-seam)
- [Custom policy](#custom-policy)
- [Recognizing tables](#recognizing-tables)
- [Configuration format](#configuration-format)

## Background

The front-end models only the constructs the DSL uses — headings, paragraphs,
lists, links, images, code spans, emphasis, and line breaks (see
[Markdown Front-End](./Markdown%20Front-End.md)). Everything else is *unmodeled*.

By default, an unmodeled construct is flattened to its raw source text so nothing
is silently lost. But some constructs are **authoring aids, not speech** — a
table listing speakers and their moods, or a mermaid diagram showing how scenes
connect. Leaking those into speech as raw text is noise. The handling policy lets
each unmodeled kind be **ignored** instead.

## Handling model

Each unmodeled kind resolves to one of two actions:

| Handling | Meaning |
| --- | --- |
| `AsRawText` (default) | Keep the construct as literal speech text, flattened from its source span. |
| `Ignore` | Drop it entirely, like a comment — it never reaches speech. |

## Kinds and defaults

`DefaultUnmodeledNodeHandlingPolicy` applies these defaults — drop authoring
aids, keep ambiguous content:

| Kind (`UnmodeledNodeKind`) | Example | Default | Why |
| --- | --- | --- | --- |
| `CodeBlock` | a fenced ` ```mermaid ` block | **Ignore** | Diagrams and code illustrate; they are not spoken |
| `ThematicBreak` | `---` | **Ignore** | A visual divider, not words |
| `Table` | `\| Speaker \| Mood \|` | **Ignore** | Organizes reference data; not spoken |
| `RawHtml` | `<div>`, `<br>` | `AsRawText` | Ambiguous; the author typed it deliberately |
| `Autolink` | `<https://example.com>` | `AsRawText` | A URL that is content |
| `Other` | any unrecognized unmodeled construct | `AsRawText` | Fallback; kept rather than silently dropped |

## The policy seam

```csharp
internal enum UnmodeledNodeKind { CodeBlock, ThematicBreak, Table, RawHtml, Autolink, Other }

internal enum UnmodeledNodeHandling { AsRawText, Ignore }

internal interface IUnmodeledNodeHandlingPolicy
{
    UnmodeledNodeHandling HandlingFor(UnmodeledNodeKind kind);
}
```

For each unmodeled node the converter asks the policy `HandlingFor(kind)`:
`Ignore` drops the node, `AsRawText` flattens it to raw text.
`DefaultUnmodeledNodeHandlingPolicy` is a singleton implementing the defaults
above. Comments are always discarded and are **not** part of this policy.

## Custom policy

Supply a custom `IUnmodeledNodeHandlingPolicy` to override any kind — for example,
keep tables as raw text while leaving the other defaults intact:

```csharp
internal sealed class KeepTablesHandlingPolicy : IUnmodeledNodeHandlingPolicy
{
    public UnmodeledNodeHandling HandlingFor(UnmodeledNodeKind kind) => kind switch
    {
        UnmodeledNodeKind.Table => UnmodeledNodeHandling.AsRawText,
        _ => DefaultUnmodeledNodeHandlingPolicy.Instance.HandlingFor(kind),
    };
}
```

Pass it when constructing the parser:

```csharp
var parser = new MarkdigMarkdownParser(new KeepTablesHandlingPolicy());
```

## Recognizing tables

To *ignore* a table, Markdig must first recognize it as one, which needs the
**pipe-table** extension. The front-end enables it, so a valid table becomes a
`Table` block (dropped by default); stray pipes that do not form a table stay
literal text. No other GitHub-flavored extensions are enabled.

## Configuration format

A DialogueDown project is configured by a **TOML** file at the project root
(`dialogue.toml`). Unmodeled-node handling lives under a `[markdown.unmodeled]`
section, mapping each kind to `"ignore"` or `"raw-text"`:

```toml
# dialogue.toml

[markdown.unmodeled]
code-block     = "ignore"    # mermaid/code: illustration, not speech
thematic-break = "ignore"
table          = "ignore"
raw-html       = "raw-text"
autolink       = "raw-text"
other          = "raw-text"
```

Omitted keys fall back to the built-in defaults. Other project concerns (speakers,
runtime, …) get their own top-level sections in the same file.

### Why TOML

Considered INI, JSON, YAML, and TOML against **sectioning**, **readability for
developers and writers**, **editor support**, and being a **standard**:

- **TOML (chosen):** explicit `[section]` headers (exactly the sectioning we
  want), INI-like clarity with real types and comments, a published standard
  (TOML 1.0; used by Cargo and `pyproject.toml`), first-class .NET parsing
  (Tomlyn, used by the .NET SDK), and schema-aware editor support (Even Better
  TOML / Taplo).
- **YAML:** very readable but whitespace-sensitive — a hazard when non-technical
  writers edit it.
- **JSON:** ubiquitous but has no comments and is noisy to hand-edit.
- **INI:** simplest, but has no formal standard, no schema/validation, and no
  nested sections.

> [!NOTE]
> This records the *format* decision only. The loader that reads `dialogue.toml`
> and builds an `IUnmodeledNodeHandlingPolicy` (and other project settings) is a
> separate, future component.

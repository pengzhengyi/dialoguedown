# Unmodeled Markdown handling

How the Markdown front-end treats Markdown constructs it does **not** model as
dialogue — each is either **kept** or **ignored** — and how to override the
defaults, in code or from a project's `dialogue.toml`.

> [!NOTE]
> Status: **implemented**. The policy, its defaults, and the
> [configuration format](#configuration-format) that overrides them are all in
> place; `TomlConfigurationLoader` reads the `[markdown.unmodeled]` section into
> `CompilerOptions`.

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

By default, an unmodeled construct is **kept** — flattened to its raw source text
so nothing is silently lost. But some constructs are **authoring aids, not
dialogue** — a table listing speakers and their moods, or a mermaid diagram
showing how scenes connect. Leaking those into the dialogue is noise. The
handling policy lets each unmodeled kind be **ignored** instead.

## Handling model

Each unmodeled kind resolves to one of two handlings:

| Handling | Meaning |
| --- | --- |
| `Keep` (default) | The construct's source text becomes dialogue text, flattened from its source span. Its **text** is kept, not its structure. |
| `Ignore` | The construct is left out of the dialogue entirely, like a comment. |

## Kinds and defaults

`DefaultUnmodeledNodeHandlingPolicy` applies these defaults — ignore authoring
aids, keep ambiguous content:

| Kind (`UnmodeledNodeKind`) | Example | Default | Why |
| --- | --- | --- | --- |
| `CodeBlock` | a fenced ` ```mermaid ` block | `Ignore` | Diagrams and code illustrate; they are not dialogue |
| `ThematicBreak` | `---` | `Ignore` | A visual divider, not words |
| `Table` | `\| Speaker \| Mood \|` | `Ignore` | Organizes reference data; not dialogue |
| `RawHtml` | `<div>`, `<br>` | `Keep` | Ambiguous; the author typed it deliberately |
| `Autolink` | `<https://example.com>` | `Keep` | A URL that is content |
| `Other` | any unrecognized unmodeled construct | `Keep` | Fallback; kept rather than silently lost |

## The policy seam

```csharp
// DialogueDown.Configuration — the vocabulary a project configures with.
public enum UnmodeledNodeKind { CodeBlock, ThematicBreak, Table, RawHtml, Autolink, Other }

public enum UnmodeledNodeHandling { Keep, Ignore }

// DialogueDown.Markdown — the seam the front-end reads.
internal interface IUnmodeledNodeHandlingPolicy
{
    UnmodeledNodeHandling HandlingFor(UnmodeledNodeKind kind);
}
```

For each unmodeled node the converter asks the policy `HandlingFor(kind)`:
`Ignore` leaves the node out, `Keep` flattens it to its source text.
`DefaultUnmodeledNodeHandlingPolicy` is a singleton implementing the defaults
above. Comments are always ignored and are **not** part of this policy.

The two enums live in `DialogueDown.Configuration`, not alongside the policy,
because they are the vocabulary a project writes in `dialogue.toml` — and because
configuration is a foundation layer that must not depend on the Markdown
front-end. The policy that reads them stays in `DialogueDown.Markdown`, so the
dependency runs one way: Markdown → Configuration.

## Custom policy

Supply a custom `IUnmodeledNodeHandlingPolicy` to override any kind — for example,
keep tables while leaving the other defaults intact:

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

Pass it when constructing the parser:

```csharp
var parser = new MarkdigMarkdownParser(new KeepTablesHandlingPolicy());
```

## Recognizing tables

To *ignore* a table, Markdig must first recognize it as one, which needs the
**pipe-table** extension. The front-end enables it, so a valid table becomes a
`Table` block (ignored by default); stray pipes that do not form a table stay
literal text. No other GitHub-flavored extensions are enabled.

## Configuration format

A DialogueDown project is configured by a **TOML** file at the project root
(`dialogue.toml`). Unmodeled-node handling lives under a `[markdown.unmodeled]`
section, mapping each kind to `"keep"` or `"ignore"`:

```toml
# dialogue.toml

[markdown.unmodeled]
code-block     = "ignore"    # mermaid/code: illustration, not dialogue
thematic-break = "ignore"
table          = "ignore"
raw-html       = "keep"
autolink       = "keep"
other          = "keep"
```

Omitted keys fall back to the built-in defaults, so the section is a list of
exceptions rather than a full replacement. `UnmodeledMarkdownNames` holds the
kebab-case names above — the single vocabulary shared by the loader and any tool
that displays the configuration, so the surfaces cannot drift. An unknown kind or
handling is rejected with a located error rather than silently falling back.
Other project concerns (speakers, mode, …) get their own sections in the same
file; see the [configuration guide](../../guide/configuration.md).

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
> This records the *format* decision only. `TomlConfigurationLoader` reads it —
> see the [Configuration Loader](./Configuration%20Loader.md) note for how a
> `dialogue.toml` becomes `CompilerOptions`.

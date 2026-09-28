# Ignored Markdown Diagnostic

> [!NOTE]
> Status: **implemented**. `DLG1114` is an `Info` note for each Markdown construct
> the front end ignores, so a table or divider that never reaches the script does
> not disappear without a word.

## Ubiquitous language

| Term | Meaning |
| --- | --- |
| **Unmodeled construct** | Markdown DialogueDown does not model as dialogue, classified as an `UnmodeledNodeKind`. |
| **Handling** | What the policy decides for a kind: `Keep` or `Ignore` (see [Unmodeled Markdown Handling](../core/Unmodeled%20Markdown%20Handling.md)). |
| **Front end** | The Markdown stage: `IMarkdownParser`, the converter, and the unmodeled-node handler. |

## Writer-facing behavior

```markdown
# The Tavern

| Rumor | Source |
| --- | --- |
| The bridge is out | The miller |

Innkeeper: Ask around.
```

```text
scene.dialogue.md(3,1): info DLG1114: This table is not dialogue, so the compiler
left it out of the script. That is expected for notes and diagrams; write it as
dialogue if it should be spoken.
```

The message names the kind in a writer's words ("table", not `Table`) and states the
fact without implying a mistake. The error-code reference offers two labeled fixes —
write it as dialogue, or remove it if it arrived by accident — and keeping it is the
triggering example unchanged. A test forbids an unlabeled first fix whenever a second
exists.

## Where it is reported

`MarkdigUnmodeledNodeHandler` owns the whole unmodeled decision — classify, ask the
policy, keep or ignore — and ignoring is where the note is written:

```csharp
public MarkdownBlock? Handle(MarkdigBlock block)
{
    if (_policy.ShouldIgnore(block))
    {
        Ignore(MarkdigUnmodeledNodeClassifier.ClassifyBlock(block), block.Span);
        return null;
    }

    if (_policy.ShouldKeep(block))
    {
        return Keep(block);
    }

    throw UnknownHandling(block);
}
```

`MarkdigMarkdownParser` builds the handler per parse with the source, the policy,
and the compilation's sink, and hands it to `MarkdigToMarkdownAstConverter`, which
converts only the constructs that are dialogue. An inline overload does the same for
unmodeled inlines.

## Key design decisions

### D1 — The parser seam takes a diagnostics context

`IMarkdownParser.Parse(string, DiagnosticsContext)` matches every other stage, so the
front end reports for itself. Returning the omissions as data would keep `Parse` pure
but make the front end the one stage that cannot report, and move its internals into
the compiler.

### D2 — One code, with the kind as an argument

A writer asks "why did my Markdown vanish?" once, so one `DLG1114` carries the kind
as `{0}` instead of a code per kind that grows with `UnmodeledNodeKind`.

### D3 — Info, not Warning

Ignoring is usually what the writer wanted; a warning would fire on every deliberate
code block. `Info` never affects `HasErrors` or an exit code.

### D4 — Report ignored inlines too

The default policy keeps every inline kind, but a project policy may ignore one, and
it gets the same account with no second code path.

### D5 — A syntax diagnostic

The `Syntax` category covers the script's surface: text that does not parse as
intended, or Markdown that never becomes dialogue. The summary appears in the
`DiagnosticCategory` documentation and on the error-code page, which must agree.

### D6 — `ShouldIgnore` and `ShouldKeep` are not negations

A handling the code has never seen answers "no" to both, so the handler throws
instead of guessing whether to keep or drop the writer's content.

## Error and boundary cases

| Case | Behavior |
| --- | --- |
| A table, code block, `---`, or link reference definition | Ignored by default; one `Info` at the construct. |
| Several ignored constructs | One `Info` each, in source order. |
| A construct the policy keeps | Nothing reported. |
| Front matter or an HTML comment | Discarded before the policy; nothing reported. |
| Inside a list item or blockquote | Reported; the converter recurses. |
| A policy answering neither keep nor ignore | `NotSupportedException` naming the construct. |

## Testability

- Handler: every ignored kind is noted once, with the writer's word and the right
  span, including kinds only a configured policy ignores — no parsing needed.
- Policy extensions: both answer "no" to an unknown handling.
- Parser and pipeline: a real parse notes a nested construct; a script with a table
  yields one `DLG1114` and still succeeds; front matter and comments stay silent.
- The error-code reference's examples are compiled: the trigger reports `DLG1114`,
  both fixes do not.

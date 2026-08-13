using DialogueDown.Configuration;
using DialogueDown.Diagnostics;
using MarkdigBlock = Markdig.Syntax.Block;
using MarkdigInline = Markdig.Syntax.Inlines.Inline;
using MarkdigSpan = Markdig.Syntax.SourceSpan;

namespace DialogueDown.Markdown;

/// <summary>
/// Decides what becomes of a Markdown construct DialogueDown does not model, and carries that
/// decision out: it asks the <see cref="IUnmodeledNodeHandlingPolicy"/> about the construct and
/// either keeps it as dialogue text or ignores it, noting every one it ignores. Gathering that here keeps
/// the conversion of modeled Markdown free of it, and lets the decision be tested without parsing
/// a script.
/// </summary>
internal sealed class MarkdigUnmodeledNodeHandler
{
    private readonly string _source;
    private readonly IUnmodeledNodeHandlingPolicy _policy;
    private readonly IDiagnosticSink _diagnostics;

    public MarkdigUnmodeledNodeHandler(
        string source, IUnmodeledNodeHandlingPolicy policy, IDiagnosticSink diagnostics)
    {
        _source = source;
        _policy = policy;
        _diagnostics = diagnostics;
    }

    /// <summary>
    /// The block that survives, or <c>null</c> when the policy ignores the construct.
    /// </summary>
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

    /// <summary>
    /// The inline that survives, or <c>null</c> when the policy ignores the construct.
    /// </summary>
    public MarkdownInline? Handle(MarkdigInline inline)
    {
        if (_policy.ShouldIgnore(inline))
        {
            Ignore(MarkdigUnmodeledNodeClassifier.ClassifyInline(inline), inline.Span);
            return null;
        }

        if (_policy.ShouldKeep(inline))
        {
            return Keep(inline);
        }

        throw UnknownHandling(inline);
    }

    // A policy that answers neither question has a handling this code has never seen — most
    // likely a new UnmodeledNodeHandling that nothing here was taught to carry out. Failing here
    // is better than silently guessing, which would keep or ignore the writer's content at random.
    private static NotSupportedException UnknownHandling(object node) =>
        new($"The handling policy chose neither to keep nor to ignore a {node.GetType().Name}. "
            + "Every UnmodeledNodeHandling must be handled here.");

    // The words a writer would use for a construct, so the note reads as prose rather than as an
    // enum name. Each fits after "This ..." in the message.
    private static string DescribeKind(UnmodeledNodeKind kind) => kind switch
    {
        UnmodeledNodeKind.CodeBlock => "code block",
        UnmodeledNodeKind.ThematicBreak => "divider",
        UnmodeledNodeKind.Table => "table",
        UnmodeledNodeKind.RawHtml => "raw HTML",
        UnmodeledNodeKind.Autolink => "autolink",
        _ => "piece of Markdown",
    };

    // Ignoring leaves nothing behind in the AST, so this note is the only sign the writer gets
    // that the construct was ever there. The kind is classified again here only to name it; the
    // decision to ignore was the policy's.
    private void Ignore(UnmodeledNodeKind kind, MarkdigSpan span) =>
        _diagnostics.Report(new Diagnostic(
            DiagnosticCatalog.IgnoredUnmodeledMarkdown, span.ToSourceSpan(), [DescribeKind(kind)]));

    // A kept construct survives as a paragraph of its exact source text, so nothing is silently
    // lost. Its text is kept, not its structure.
    private MarkdownBlock Keep(MarkdigBlock block)
    {
        var span = block.Span.ToSourceSpan();
        return new Paragraph([new TextInline(Slice(block.Span), span)], span);
    }

    // A kept inline survives as its exact source text, so no dialogue content is lost.
    private MarkdownInline Keep(MarkdigInline inline) =>
        new TextInline(Slice(inline.Span), inline.Span.ToSourceSpan());

    private string Slice(MarkdigSpan span) => _source.Substring(span.Start, span.Length);
}

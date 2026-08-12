using MarkdigBlock = Markdig.Syntax.Block;
using MarkdigInline = Markdig.Syntax.Inlines.Inline;
using MarkdigSpan = Markdig.Syntax.SourceSpan;

namespace DialogueDown.Markdown;

/// <summary>
/// Decides what becomes of a Markdown construct DialogueDown does not model, and carries that
/// decision out: it asks the <see cref="IUnmodeledNodeHandlingPolicy"/> about the construct and
/// either degrades it to raw speech text or drops it. Gathering that here keeps the conversion of
/// modeled Markdown free of it, and lets the decision be tested without parsing a script.
/// </summary>
internal sealed class MarkdigUnmodeledNodeHandler
{
    private readonly string _source;
    private readonly IUnmodeledNodeHandlingPolicy _policy;

    public MarkdigUnmodeledNodeHandler(string source, IUnmodeledNodeHandlingPolicy policy)
    {
        _source = source;
        _policy = policy;
    }

    /// <summary>
    /// The block that survives as raw text, or <c>null</c> when the policy drops the construct.
    /// </summary>
    public MarkdownBlock? Handle(MarkdigBlock block)
    {
        if (_policy.ShouldIgnore(block))
        {
            return null;
        }

        if (_policy.ShouldKeep(block))
        {
            return Degrade(block);
        }

        throw UnknownHandling(block);
    }

    /// <summary>
    /// The inline that survives as raw text, or <c>null</c> when the policy drops the construct.
    /// </summary>
    public MarkdownInline? Handle(MarkdigInline inline)
    {
        if (_policy.ShouldIgnore(inline))
        {
            return null;
        }

        if (_policy.ShouldKeep(inline))
        {
            return Degrade(inline);
        }

        throw UnknownHandling(inline);
    }

    // A policy that answers neither question has a handling this code has never seen — most
    // likely a new UnmodeledNodeHandling that nothing here was taught to carry out. Failing here
    // is better than silently guessing, which would drop or keep the writer's content at random.
    private static NotSupportedException UnknownHandling(object node) =>
        new($"The handling policy chose neither to keep nor to ignore a {node.GetType().Name}. "
            + "Every UnmodeledNodeHandling must be handled here.");

    // A construct we do not model (raw HTML, or anything DialogueDown does not recognize)
    // survives as a paragraph of its exact source text, so nothing is silently dropped.
    private MarkdownBlock Degrade(MarkdigBlock block)
    {
        var span = block.Span.ToSourceSpan();
        return new Paragraph([new TextInline(Slice(block.Span), span)], span);
    }

    // A construct we do not model (autolink, raw HTML, ...) survives as its exact source text so
    // no spoken content is lost.
    private MarkdownInline Degrade(MarkdigInline inline) =>
        new TextInline(Slice(inline.Span), inline.Span.ToSourceSpan());

    private string Slice(MarkdigSpan span) => _source.Substring(span.Start, span.Length);
}

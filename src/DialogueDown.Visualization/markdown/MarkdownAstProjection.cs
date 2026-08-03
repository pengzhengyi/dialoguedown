using DialogueDown.Common;
using DialogueDown.Markdown;

namespace DialogueDown.Visualization;

/// <summary>
/// Projects the Markdown AST for display: it labels every node type, yields each
/// node's children, and attaches the original source text each node was produced
/// from (sliced from the node's span). The AST has no single common base
/// (<see cref="MarkdownDocument"/>, blocks, inlines, and list items are separate
/// families), so the node type is <see cref="object"/> and each node is matched
/// by its runtime type.
/// </summary>
internal sealed class MarkdownAstProjection : INodeProjection<object>
{
    // Semantic categories: a stable, cross-stage vocabulary the renderer maps to
    // colors. Later stages reuse these names so corresponding concepts share a
    // color — e.g. a code span becomes a game call, so both are "call".
    private const string DocumentCategory = "document";
    private const string StructureCategory = "structure";
    private const string SpeechCategory = "speech";
    private const string TextCategory = "text";
    private const string ChoiceCategory = "choice";
    private const string JumpCategory = "jump";
    private const string MediaCategory = "media";
    private const string CallCategory = "call";
    private const string StylingCategory = "styling";
    private const string BreakCategory = "break";

    private readonly string _source;

    public MarkdownAstProjection(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _source = source;
    }

    public string Title => "Markdown AST";

    public string Description =>
        "The Markdown syntax tree parsed from the source — its document, blocks " +
        "(headings, paragraphs, lists, blockquotes), and inline spans, each tied to the text it came from.";

    public NodeDescription Describe(object node)
    {
        ArgumentNullException.ThrowIfNull(node);
        NodeDescription description = node switch
        {
            MarkdownDocument => new("Document", source: _source, category: DocumentCategory),
            Heading heading => new(
                $"Heading (H{heading.Level})",
                [new("level", $"{heading.Level}"), SpanAttribute(heading.Span)],
                Slice(heading.Span),
                StructureCategory),
            Paragraph paragraph => new(
                "Paragraph", [SpanAttribute(paragraph.Span)], Slice(paragraph.Span), SpeechCategory),
            ListBlock list => new(
                list.IsOrdered ? "List (ordered)" : "List (unordered)",
                [SpanAttribute(list.Span)],
                Slice(list.Span),
                ChoiceCategory),
            ListItem item => new("List item", [SpanAttribute(item.Span)], Slice(item.Span), ChoiceCategory),
            QuoteBlock quote => new(
                "Block quote", [SpanAttribute(quote.Span)], Slice(quote.Span), StructureCategory),
            TextInline text => new(
                "Text", [new("text", text.Text), SpanAttribute(text.Span)], Slice(text.Span), TextCategory),
            LinkInline link => new(
                "Link",
                [new("target", link.Target), new("label", InlineText(link.Label)), SpanAttribute(link.Span)],
                Slice(link.Span),
                JumpCategory),
            ImageInline image => new(
                "Image",
                [new("source", image.Source), new("alt", InlineText(image.Alt)), SpanAttribute(image.Span)],
                Slice(image.Span),
                MediaCategory),
            CodeSpanInline code => new(
                "Code span",
                [new("content", code.Content), SpanAttribute(code.Span)],
                Slice(code.Span),
                CallCategory),
            EmphasisInline emphasis => new(
                $"Emphasis ({emphasis.Kind})", [SpanAttribute(emphasis.Span)], Slice(emphasis.Span), StylingCategory),
            LineBreak lineBreak => new(
                lineBreak.IsHard ? "Line break (hard)" : "Line break (soft)",
                [SpanAttribute(lineBreak.Span)],
                Slice(lineBreak.Span),
                BreakCategory),
            _ => throw new ArgumentException(
                $"Unsupported Markdown AST node type '{node.GetType().Name}'.", nameof(node)),
        };

        // Attach the structured span centrally: the root spans the whole document; every
        // block, inline, and list item carries its own clamped span.
        return description with { Span = SpanOf(node) };
    }

    public IEnumerable<object> Neighbors(object node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return node switch
        {
            MarkdownDocument document => document.Blocks,
            Heading heading => heading.Inlines,
            Paragraph paragraph => paragraph.Inlines,
            ListBlock list => list.Items,
            ListItem item => item.Blocks,
            QuoteBlock quote => quote.Blocks,
            EmphasisInline emphasis => emphasis.Children,
            _ => [],
        };
    }

    private static DisplayAttribute SpanAttribute(SourceSpan span) =>
        new("span", $"[{span.Start}, {span.End})");

    // A link label or image alt is a run of inline nodes; flatten it to plain text for
    // the attribute display. Styling delimiters are dropped (the node's own span still
    // points at the exact source), so `[**bold**](url)` shows its label as `bold`.
    private static string InlineText(IReadOnlyList<MarkdownInline> inlines) =>
        string.Concat(inlines.Select(InlineText));

    private static string InlineText(MarkdownInline inline) => inline switch
    {
        TextInline text => text.Text,
        CodeSpanInline code => code.Content,
        EmphasisInline emphasis => InlineText(emphasis.Children),
        LinkInline link => InlineText(link.Label),
        ImageInline image => InlineText(image.Alt),
        LineBreak => " ",
        _ => string.Empty,
    };

    // Markdig source locations can occasionally run past the end of the string;
    // clamp defensively so a diagnostics view never throws on a stray span.
    private string Slice(SourceSpan span)
    {
        var start = Math.Clamp(span.Start, 0, _source.Length);
        var end = Math.Clamp(span.End, start, _source.Length);
        return _source[start..end];
    }

    // The node's editable source range: the whole document for the root, and the clamped
    // span for a block, inline, or list item.
    private DisplaySpan? SpanOf(object node) => node switch
    {
        MarkdownDocument => new DisplaySpan(0, _source.Length),
        MarkdownBlock block => ToSpan(block.Span),
        MarkdownInline inline => ToSpan(inline.Span),
        ListItem item => ToSpan(item.Span),
        _ => null,
    };

    // The structured, clamped span a client splices with — the same clamping as Slice, so a
    // node's span and its sliced source always agree. An empty span yields none.
    private DisplaySpan? ToSpan(SourceSpan span)
    {
        if (span.IsEmpty)
        {
            return null;
        }

        var start = Math.Clamp(span.Start, 0, _source.Length);
        var end = Math.Clamp(span.End, start, _source.Length);
        return new DisplaySpan(start, end);
    }
}

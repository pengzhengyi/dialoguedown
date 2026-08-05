using DialogueDown.Common;
using MarkdigBlock = Markdig.Syntax.Block;
using MarkdigCodeInline = Markdig.Syntax.Inlines.CodeInline;
using MarkdigContainerInline = Markdig.Syntax.Inlines.ContainerInline;
using MarkdigDocument = Markdig.Syntax.MarkdownDocument;
using MarkdigEmphasisInline = Markdig.Syntax.Inlines.EmphasisInline;
using MarkdigHeadingBlock = Markdig.Syntax.HeadingBlock;
using MarkdigHtmlBlock = Markdig.Syntax.HtmlBlock;
using MarkdigHtmlBlockType = Markdig.Syntax.HtmlBlockType;
using MarkdigHtmlInline = Markdig.Syntax.Inlines.HtmlInline;
using MarkdigInline = Markdig.Syntax.Inlines.Inline;
using MarkdigLineBreakInline = Markdig.Syntax.Inlines.LineBreakInline;
using MarkdigLinkInline = Markdig.Syntax.Inlines.LinkInline;
using MarkdigListBlock = Markdig.Syntax.ListBlock;
using MarkdigListItemBlock = Markdig.Syntax.ListItemBlock;
using MarkdigLiteralInline = Markdig.Syntax.Inlines.LiteralInline;
using MarkdigParagraphBlock = Markdig.Syntax.ParagraphBlock;
using MarkdigQuoteBlock = Markdig.Syntax.QuoteBlock;
using MarkdigSpan = Markdig.Syntax.SourceSpan;
using MarkdigYamlFrontMatterBlock = Markdig.Extensions.Yaml.YamlFrontMatterBlock;

namespace DialogueDown.Markdown;

/// <summary>
/// Converts a Markdig syntax tree into our own Markdown AST. Keeping this
/// translation in one place is what isolates the rest of the compiler from the
/// Markdig library. A fresh instance is created per parse and holds the source
/// text and a handling policy, so an unmodeled construct can be sliced back to
/// raw text or dropped, as the policy directs.
/// </summary>
internal sealed class MarkdigToMarkdownAstConverter
{
    private readonly string _source;
    private readonly IUnmodeledNodeHandlingPolicy _policy;

    public MarkdigToMarkdownAstConverter(string source, IUnmodeledNodeHandlingPolicy policy)
    {
        _source = source;
        _policy = policy;
    }

    public MarkdownDocument Convert(MarkdigDocument document) => new(ConvertBlocks(document));

    private static bool IsComment(MarkdigBlock block) =>
        block is MarkdigHtmlBlock html && html.Type == MarkdigHtmlBlockType.Comment;

    private static bool IsFrontMatter(MarkdigBlock block) =>
        block is MarkdigYamlFrontMatterBlock;

    private static bool IsComment(MarkdigInline inline) =>
        inline is MarkdigHtmlInline html && html.Tag.StartsWith("<!--", StringComparison.Ordinal);

    private static SourceSpan ConvertSpan(MarkdigSpan span) => new(span.Start, span.Length);

    // A literal's unescaped content sits at its raw Span, shifted past any stripped leading
    // escape. Markdig's Content.Start is unreliable — for a soft-wrapped paragraph it rebuilds
    // the content into a private buffer, so Content.Start is relative to that buffer (0), not
    // the source — but the Span is always absolute and Content.Length is exact, so anchor the
    // content at Span.Start plus the leading-escape delta (Span.Length - Content.Length).
    private static SourceSpan ContentSpanOf(MarkdigLiteralInline literal) =>
        new(literal.Span.Start + (literal.Span.Length - literal.Content.Length), literal.Content.Length);

    private static EmphasisKind EmphasisKindFor(MarkdigEmphasisInline emphasis) =>
        (emphasis.DelimiterChar, emphasis.DelimiterCount) switch
        {
            ('~', _) => EmphasisKind.Strikethrough,
            (_, 2) => EmphasisKind.Bold,
            _ => EmphasisKind.Italic,
        };

    private IReadOnlyList<MarkdownBlock> ConvertBlocks(IEnumerable<MarkdigBlock> blocks)
    {
        var converted = new List<MarkdownBlock>();
        foreach (var block in blocks)
        {
            if (IsComment(block) || IsFrontMatter(block))
            {
                continue;
            }

            var mapped = ConvertBlock(block);
            if (mapped is not null)
            {
                converted.Add(mapped);
            }
        }

        return converted;
    }

    private MarkdownBlock? ConvertBlock(MarkdigBlock block) => block switch
    {
        MarkdigHeadingBlock heading => ConvertHeading(heading),
        MarkdigParagraphBlock paragraph => ConvertParagraph(paragraph),
        MarkdigListBlock list => ConvertList(list),
        MarkdigQuoteBlock quote => ConvertQuote(quote),
        _ => HandleUnmodeledBlock(block),
    };

    private MarkdownBlock? HandleUnmodeledBlock(MarkdigBlock block) =>
        HandlingForBlock(block) switch
        {
            UnmodeledNodeHandling.Ignore => null,
            _ => FlattenBlock(block), // AsRawText: keep the construct as raw text.
        };

    private UnmodeledNodeHandling HandlingForBlock(MarkdigBlock block) =>
        _policy.HandlingFor(MarkdigUnmodeledNodeClassifier.ClassifyBlock(block));

    private ListBlock ConvertList(MarkdigListBlock block)
    {
        var items = new List<ListItem>();
        foreach (var child in block)
        {
            items.Add(ConvertListItem((MarkdigListItemBlock)child));
        }

        return new ListBlock(block.IsOrdered, items, ConvertSpan(block.Span));
    }

    private ListItem ConvertListItem(MarkdigListItemBlock block) =>
        new(ConvertBlocks(block), ConvertSpan(block.Span));

    // A blockquote is kept as a structural wrapper around its inner blocks (not flattened) so a
    // later stage can recognize a marker-headed quote as a block conditional.
    private QuoteBlock ConvertQuote(MarkdigQuoteBlock block) =>
        new(ConvertBlocks(block), ConvertSpan(block.Span));

    private Heading ConvertHeading(MarkdigHeadingBlock block) =>
        // A parsed heading always has an inline container (empty for a bare '##').
        new(block.Level, ConvertInlines(block.Inline!), ConvertSpan(block.Span));

    private Paragraph ConvertParagraph(MarkdigParagraphBlock block) =>
        // A parsed paragraph always has inline content, so Inline is never null here.
        new(ConvertInlines(block.Inline!), ConvertSpan(block.Span));

    private Paragraph FlattenBlock(MarkdigBlock block)
    {
        // A construct we do not model (blockquote, raw HTML, ...) survives as a
        // paragraph of its exact source text, so nothing is silently dropped.
        var span = ConvertSpan(block.Span);
        return new Paragraph([new TextInline(Slice(block.Span), span)], span);
    }

    private IReadOnlyList<MarkdownInline> ConvertInlines(MarkdigContainerInline container)
    {
        var inlines = new List<MarkdownInline>();
        foreach (var inline in container)
        {
            if (IsComment(inline))
            {
                continue;
            }

            var mapped = ConvertInline(inline);
            if (mapped is not null)
            {
                inlines.Add(mapped);
            }
        }

        return inlines;
    }

    private MarkdownInline? ConvertInline(MarkdigInline inline) => inline switch
    {
        MarkdigLiteralInline literal => new TextInline(
            literal.Content.ToString(), ConvertSpan(literal.Span), ContentSpanOf(literal)),
        MarkdigEmphasisInline emphasis => ConvertEmphasis(emphasis),
        MarkdigLinkInline link when !link.IsImage => ConvertLink(link),
        MarkdigLinkInline image => ConvertImage(image),
        MarkdigCodeInline code => new CodeSpanInline(code.Content, ConvertSpan(code.Span)),
        MarkdigLineBreakInline lineBreak => new LineBreak(lineBreak.IsHard, ConvertSpan(lineBreak.Span)),
        _ => HandleUnmodeledInline(inline),
    };

    private MarkdownInline? HandleUnmodeledInline(MarkdigInline inline) =>
        HandlingForInline(inline) switch
        {
            UnmodeledNodeHandling.Ignore => null,
            _ => FlattenInline(inline), // AsRawText: keep the construct as raw text.
        };

    private UnmodeledNodeHandling HandlingForInline(MarkdigInline inline) =>
        _policy.HandlingFor(MarkdigUnmodeledNodeClassifier.ClassifyInline(inline));

    private TextInline FlattenInline(MarkdigInline inline)
    {
        // A construct we do not model (autolink, raw HTML, ...) survives as its
        // exact source text so no spoken content is lost.
        var span = ConvertSpan(inline.Span);
        return new TextInline(Slice(inline.Span), span);
    }

    private LinkInline ConvertLink(MarkdigLinkInline link) =>
        // A parsed inline link always has a URL (empty string when omitted), never null.
        new(link.Url!, ConvertInlines(link), ConvertSpan(link.Span));

    private EmphasisInline ConvertEmphasis(MarkdigEmphasisInline emphasis) =>
        new(EmphasisKindFor(emphasis), ConvertInlines(emphasis), ConvertSpan(emphasis.Span));

    private ImageInline ConvertImage(MarkdigLinkInline image) =>
        // Same shape as a link: the URL is the source and the bracket content is the alt,
        // kept as inline nodes so it can carry styling.
        new(image.Url!, ConvertInlines(image), ConvertSpan(image.Span));

    private string Slice(MarkdigSpan span) => _source.Substring(span.Start, span.Length);
}

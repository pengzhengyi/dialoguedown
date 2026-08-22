using DialogueDown.Common;
using DialogueDown.Markdown;

namespace DialogueDown.Tests.Support;

/// <summary>
/// Walks a <see cref="MarkdownDocument"/> and yields every span in it, each beside a name for
/// whatever carries it.
/// </summary>
/// <remarks>
/// The Markdown AST has no shared walker, because nothing in the compiler needs one: each stage
/// knows the shapes it handles. A test that quantifies over the whole tree does need one, so it
/// lives here rather than widening the library's surface for a test.
/// </remarks>
internal static class MarkdownSpans
{
    /// <summary>Every span in the document, each beside a name for what carries it.</summary>
    public static IEnumerable<(string Subject, SourceSpan Span)> Of(MarkdownDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return document.Blocks.SelectMany(InBlock);
    }

    private static IEnumerable<(string, SourceSpan)> InBlock(MarkdownBlock block)
    {
        yield return (block.GetType().Name, block.Span);

        var children = block switch
        {
            Heading heading => heading.Inlines.SelectMany(InInline),
            Paragraph paragraph => paragraph.Inlines.SelectMany(InInline),
            QuoteBlock quote => quote.Blocks.SelectMany(InBlock),
            ListBlock list => list.Items.SelectMany(InItem),
            _ => [],
        };

        foreach (var child in children)
        {
            yield return child;
        }
    }

    private static IEnumerable<(string, SourceSpan)> InItem(ListItem item)
    {
        yield return (nameof(ListItem), item.Span);

        foreach (var child in item.Blocks.SelectMany(InBlock))
        {
            yield return child;
        }
    }

    private static IEnumerable<(string, SourceSpan)> InInline(MarkdownInline inline)
    {
        yield return (inline.GetType().Name, inline.Span);

        var children = inline switch
        {
            EmphasisInline emphasis => emphasis.Children.SelectMany(InInline),
            LinkInline link => link.Label.SelectMany(InInline),
            ImageInline image => image.Alt.SelectMany(InInline),
            _ => Enumerable.Empty<(string, SourceSpan)>(),
        };

        foreach (var child in children)
        {
            yield return child;
        }
    }
}

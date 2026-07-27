using System.Text;
using DialogueDown.Common;

namespace DialogueDown.Markdown;

/// <summary>
/// Reusable transformations over Markdown inline content, shared by any construct that reshapes
/// it.
/// </summary>
internal static class MarkdownInlineExtensions
{
    /// <summary>
    /// The text inline with any leading whitespace removed, re-anchoring its span. Returns the
    /// same inline when it has none, or <c>null</c> when it is entirely whitespace (nothing is
    /// left).
    /// </summary>
    public static TextInline? TrimLeadingWhitespace(this TextInline text)
    {
        if (!text.Text.HasLeadingWhitespace())
        {
            return text;
        }

        var trimmed = text.Text.TrimStart();
        if (trimmed.Length == 0)
        {
            return null;
        }

        var removed = text.Text.Length - trimmed.Length;
        return new TextInline(
            trimmed,
            new SourceSpan(text.Span.Start + removed, trimmed.Length),
            new SourceSpan(text.ContentSpan.Start + removed, trimmed.Length));
    }

    /// <summary>
    /// The inlines with any leading whitespace removed from the first text inline (a
    /// whitespace-only leading inline is dropped). This lets a caller that peels a leading
    /// inline — such as a random choice's weight — leave clean content behind, so the remainder
    /// reads as if the peeled inline were never there.
    /// </summary>
    public static IReadOnlyList<MarkdownInline> TrimLeadingWhitespace(
        this IEnumerable<MarkdownInline> inlines)
    {
        var list = inlines as IReadOnlyList<MarkdownInline> ?? inlines.ToList();
        if (list is not [TextInline head, ..])
        {
            return list;
        }

        var trimmed = head.TrimLeadingWhitespace();
        if (ReferenceEquals(trimmed, head))
        {
            return list;
        }

        var rest = list.Skip(1);
        return trimmed is null ? rest.ToList() : [trimmed, .. rest];
    }

    /// <summary>
    /// The plain, unstyled text of this inline: a text inline's text, or an emphasis inline's
    /// children flattened the same way — styling is dropped, only the words remain. Returns
    /// <c>null</c> when the inline holds a functional element (a code span, link, image, or break)
    /// that has no plain-text form.
    /// </summary>
    public static string? PlainText(this MarkdownInline inline) => inline switch
    {
        TextInline text => text.Text,
        EmphasisInline emphasis => emphasis.Children.PlainText(),
        _ => null,
    };

    /// <summary>
    /// The plain text of a run of inlines, concatenated (see
    /// <see cref="PlainText(MarkdownInline)"/>), or <c>null</c> when any inline in the run has no
    /// plain-text form.
    /// </summary>
    public static string? PlainText(this IEnumerable<MarkdownInline> inlines)
    {
        var builder = new StringBuilder();
        foreach (var inline in inlines)
        {
            if (inline.PlainText() is not { } text)
            {
                return null;
            }

            builder.Append(text);
        }

        return builder.ToString();
    }
}

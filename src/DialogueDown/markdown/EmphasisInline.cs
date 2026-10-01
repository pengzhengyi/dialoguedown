using DialogueDown.Common;
namespace DialogueDown.Markdown;

/// <summary>
/// Emphasized text: italic, bold, or strikethrough (see <see cref="Kind"/>). <see cref="Children"/>
/// are the parsed inline contents, so a query, jump, image, or nested emphasis
/// inside keeps its structure. This layer records only that the text is styled;
/// how it renders is decided downstream.
/// </summary>
internal sealed record EmphasisInline(
    EmphasisKind Kind,
    IReadOnlyList<MarkdownInline> Children,
    SourceSpan Span) : MarkdownInline(Span);

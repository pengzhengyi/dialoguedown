using DialogueDown.Common;
namespace DialogueDown.Markdown;

/// <summary>
/// A list of items. <see cref="IsOrdered"/> records whether the source used
/// numbers (<c>1.</c>) or bullets (<c>-</c>); for a choice, it decides whether the
/// options must be offered in order.
/// </summary>
internal sealed record ListBlock(bool IsOrdered, IReadOnlyList<ListItem> Items, SourceSpan Span)
    : MarkdownBlock(Span);

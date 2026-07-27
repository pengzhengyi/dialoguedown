using DialogueDown.Common;
namespace DialogueDown.Markdown;

/// <summary>
/// Base type for inline Markdown content — the pieces that make up a line of
/// text, such as plain text, links, and code spans.
/// </summary>
internal abstract record MarkdownInline(SourceSpan Span) : ISpanned;

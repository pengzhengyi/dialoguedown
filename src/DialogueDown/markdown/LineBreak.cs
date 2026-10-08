using DialogueDown.Common;
namespace DialogueDown.Markdown;

/// <summary>
/// A line break inside a paragraph. <see cref="IsHard"/> tells a hard break (two
/// trailing spaces or a trailing backslash) apart from a soft break (a plain
/// newline). This layer only records the break; the dialogue compiler starts a new
/// line at a hard break and keeps a soft break inside the same line, as a place
/// display may wrap.
/// </summary>
internal sealed record LineBreak(bool IsHard, SourceSpan Span) : MarkdownInline(Span);

using DialogueDown.Common;
using MarkdigSpan = Markdig.Syntax.SourceSpan;

namespace DialogueDown.Markdown;

/// <summary>
/// Translates a Markdig source range into ours. Shared so every translation of a range agrees,
/// wherever in the front end it happens.
/// </summary>
internal static class MarkdigSpanExtensions
{
    public static SourceSpan ToSourceSpan(this MarkdigSpan span) => new(span.Start, span.Length);
}

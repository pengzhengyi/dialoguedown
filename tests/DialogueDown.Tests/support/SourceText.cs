using DialogueDown.Common;

namespace DialogueDown.Tests.Support;

/// <summary>Reads the text a span covers, so a test can say what a node or report points at.</summary>
internal static class SourceText
{
    /// <summary>The text <paramref name="span"/> covers in <paramref name="source"/>.</summary>
    /// <param name="source">The script the span was taken from.</param>
    /// <param name="span">Where the text sits.</param>
    /// <returns>The covered text; empty for an empty span.</returns>
    public static string At(string source, SourceSpan span) => source.Substring(span.Start, span.Length);
}

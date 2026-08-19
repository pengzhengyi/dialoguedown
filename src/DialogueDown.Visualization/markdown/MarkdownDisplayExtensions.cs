using DialogueDown.Markdown;
using DialogueDown.Visualization.Display;

namespace DialogueDown.Visualization.Markdown;

/// <summary>
/// Ergonomic entry point for the Markdown stage: turns a parsed
/// <see cref="MarkdownDocument"/> into a display graph without naming the
/// projection. The <paramref name="source"/> is the original script text, so each
/// node can carry the snippet it was produced from.
/// </summary>
internal static class MarkdownDisplayExtensions
{
    public static DisplayGraph ToDisplayGraph(this MarkdownDocument document, string source) =>
        GraphWalk.Walk<object>(document, new MarkdownAstProjection(source));
}

using MarkdigBlock = Markdig.Syntax.Block;
using MarkdigInline = Markdig.Syntax.Inlines.Inline;

namespace DialogueDown.Markdown;

/// <summary>
/// Asks an <see cref="IUnmodeledNodeHandlingPolicy"/> about a Markdig node directly, so a caller
/// states what it wants to know — "should this be kept?" — instead of classifying the node and
/// comparing handlings itself. The two questions are deliberately separate rather than one
/// negating the other, so a caller can tell a handling it does not understand from either answer.
/// </summary>
internal static class UnmodeledNodeHandlingPolicyExtensions
{
    public static bool ShouldIgnore(this IUnmodeledNodeHandlingPolicy policy, MarkdigBlock block) =>
        policy.HandlingFor(MarkdigUnmodeledNodeClassifier.ClassifyBlock(block))
            == UnmodeledNodeHandling.Ignore;

    public static bool ShouldIgnore(this IUnmodeledNodeHandlingPolicy policy, MarkdigInline inline) =>
        policy.HandlingFor(MarkdigUnmodeledNodeClassifier.ClassifyInline(inline))
            == UnmodeledNodeHandling.Ignore;

    public static bool ShouldKeep(this IUnmodeledNodeHandlingPolicy policy, MarkdigBlock block) =>
        policy.HandlingFor(MarkdigUnmodeledNodeClassifier.ClassifyBlock(block))
            == UnmodeledNodeHandling.AsRawText;

    public static bool ShouldKeep(this IUnmodeledNodeHandlingPolicy policy, MarkdigInline inline) =>
        policy.HandlingFor(MarkdigUnmodeledNodeClassifier.ClassifyInline(inline))
            == UnmodeledNodeHandling.AsRawText;
}

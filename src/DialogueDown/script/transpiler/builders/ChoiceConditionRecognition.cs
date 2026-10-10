using DialogueDown.Common;
using DialogueDown.Markdown;
using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Transpiler.Builders;

/// <summary>
/// Splits a leading condition off a choice list item, so the condition guards the whole option
/// rather than its first line. It reads the condition with
/// <see cref="ConditionReader.TryReadLeading"/> on the item's first paragraph and returns the
/// item's blocks with the condition removed; the player and random choice builders both call it,
/// before the option body — and, for a random option, its weight — is built.
/// </summary>
/// <remarks>
/// <code>
/// - `Alice.HasKey?` Alice: I'll unlock it.
/// </code>
/// </remarks>
internal static class ChoiceConditionRecognition
{
    /// <summary>
    /// The list item's blocks with a leading condition removed from its first paragraph;
    /// <paramref name="condition"/> is the condition, or <c>null</c> when the option is unconditional
    /// and the blocks are returned unchanged.
    /// </summary>
    public static IReadOnlyList<MarkdownBlock> WithoutLeadingCondition(ListItem item, out Condition? condition)
    {
        if (item.Blocks is [Paragraph paragraph, ..]
            && ConditionReader.TryReadLeading(paragraph.Inlines, out var found, out var remainder))
        {
            condition = found;
            var head = remainder.Count > 0
                ? new Paragraph(remainder, SourceSpan.Covering(remainder))
                : null;
            return item.Blocks.ReplaceOrRemoveAt(0, head);
        }

        condition = null;
        return item.Blocks;
    }
}

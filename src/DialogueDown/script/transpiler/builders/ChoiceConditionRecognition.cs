using DialogueDown.Common;
using DialogueDown.Markdown;
using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Transpiler.Builders;

/// <summary>
/// Peels a leading condition off a choice list item, so the condition guards the whole option
/// rather than its first line. It reuses the shared <see cref="ConditionReader.TryPeel"/> on the
/// item's first paragraph and returns the item's blocks with the condition removed; the player
/// and random choice builders both call it, before the option body — and, for a random option,
/// its weight — is built.
/// </summary>
internal static class ChoiceConditionRecognition
{
    /// <summary>
    /// The list item's blocks with a leading condition peeled off its first paragraph;
    /// <paramref name="condition"/> is the condition, or <c>null</c> when the option is unconditional
    /// and the blocks are returned unchanged.
    /// </summary>
    public static IReadOnlyList<MarkdownBlock> Peel(ListItem item, out Condition? condition)
    {
        if (item.Blocks is [Paragraph paragraph, ..]
            && ConditionReader.TryPeel(paragraph.Inlines, out var found, out var remainder))
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

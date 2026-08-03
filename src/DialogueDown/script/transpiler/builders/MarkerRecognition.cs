using DialogueDown.Markdown;

namespace DialogueDown.Script.Transpiler.Builders;

/// <summary>
/// Recognizes a paragraph's inlines as a block-conditional marker in the two-span form — a bare
/// keyword code span (<c>`if`</c>/<c>`elseif`</c>/<c>`else`</c>) optionally followed by the
/// verbatim condition span. It only classifies: a leading marker keyword yields a
/// <see cref="BranchMarker"/> carrying its captured condition and remainder, and anything else
/// yields null so the block reads as ordinary content. The condition is peeled by the shared
/// <see cref="ConditionReader.TryPeel"/>, never re-derived here; judging a marker well-formed —
/// and reporting one that is not — belongs to a later stage.
/// </summary>
internal static class MarkerRecognition
{
    /// <summary>
    /// The <see cref="BranchMarker"/> when <paramref name="inlines"/> leads with a marker keyword
    /// span, or null when it does not — an ordinary line, or a one-span <c>`if Rich?`</c> that is a
    /// condition on the key "if Rich" rather than a marker.
    /// </summary>
    public static BranchMarker? Read(IReadOnlyList<MarkdownInline> inlines)
    {
        if (inlines is not [CodeSpanInline keyword, ..])
        {
            return null;
        }

        if (KeywordKind(keyword.Content.Trim()) is not { } kind)
        {
            return null;
        }

        var afterKeyword = inlines.Skip(1).TrimLeadingWhitespace();
        if (ConditionReader.TryPeel(afterKeyword, out var condition, out var remainder))
        {
            return new BranchMarker(kind, condition, remainder);
        }

        return new BranchMarker(kind, Condition: null, Remainder: afterKeyword);
    }

    private static BranchKind? KeywordKind(string keyword) => keyword switch
    {
        "if" => BranchKind.If,
        "elseif" => BranchKind.ElseIf,
        "else" => BranchKind.Else,
        _ => null,
    };
}

namespace DialogueDown.Script.Ast;

/// <summary>
/// Flattens a run of inline fragments — a link or jump label, an image alt, a scene heading —
/// to plain text for a compact label or attribute. Each node's own span still points at the
/// exact source; this is only the readable text.
/// </summary>
internal static class InlineText
{
    /// <summary>The plain text of <paramref name="fragments"/>, concatenated in order.</summary>
    public static string Of(IReadOnlyList<InlineFragment> fragments) =>
        string.Concat(fragments.Select(Of));

    private static string Of(InlineFragment fragment) => fragment switch
    {
        Text text => text.Content,
        StyledText styled => Of(styled.Children),
        Link link => Of(link.Label),
        Jump jump => Of(jump.Label),
        Image image => Of(image.Alt),
        LineBreak => " ",
        _ => string.Empty,
    };
}

// An alias: importing the whole speech namespace would put its SpeechStyle in scope beside
// this namespace's own SpeechStyle.
using PlaybookSpeechText = DialogueDown.Playbook.Speech.SpeechText;

namespace DialogueDown.Script.Ast;

/// <summary>
/// Flattens a sequence of inline fragments — a link or jump label, an image alt, a scene heading —
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
        // A query stands for a value only a running game can supply, so flattening names it, as
        // {Gold} for the key Gold. The wording comes from the format rather than from here,
        // because the same query read off a compiled playbook has to read the same way.
        Query query => PlaybookSpeechText.PlaceholderFor(query.Key),
        _ => string.Empty,
    };
}

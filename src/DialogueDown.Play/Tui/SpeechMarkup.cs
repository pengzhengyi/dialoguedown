using System.Collections.Immutable;
using DialogueDown.Playbook.Speech;
using Spectre.Console;

namespace DialogueDown.Play.Tui;

/// <summary>
/// Reads speech as Spectre markup for a terminal: emphasis as itself, every literal escaped, a
/// query as its key in braces, and a link or image as its words. The rich sibling of
/// <see cref="SpeechText"/>, which flattens the same fragments to plain text.
/// </summary>
/// <remarks>
/// A tag describes a line and a command is something the host does, so neither contributes text.
/// Every other kind is named here rather than defaulted, so a kind added to the format arrives as
/// a failure instead of silently vanishing from the player's screen.
/// </remarks>
public static class SpeechMarkup
{
    /// <summary>Renders <paramref name="speech"/> as Spectre markup.</summary>
    /// <param name="speech">The fragments to render, in the order they are said.</param>
    /// <returns>The markup, empty when nothing is said.</returns>
    public static string Of(ImmutableArray<SpeechFragment> speech) =>
        string.Concat(speech.Select(Of));

    private static string Of(SpeechFragment fragment) => fragment switch
    {
        TextFragment text => Markup.Escape(text.Text),
        StyledTextFragment styled => Styled(styled),
        // A link or an image gives its words, not its address.
        LinkFragment link => Of(link.Label),
        ImageFragment image => Of(image.Alt),
        // A break records where the source wrapped, not a break the writer asked for.
        LineBreakFragment => " ",
        QueryFragment query => Markup.Escape("{" + query.Key + "}"),
        DefaultCommandFragment or CustomCommandFragment or TagFragment => string.Empty,
        _ => throw new NotSupportedException(
            $"No terminal rendering is defined for {fragment.GetType().Name}."),
    };

    private static string Styled(StyledTextFragment styled) => styled.Style switch
    {
        SpeechStyle.Italic => $"[italic]{Of(styled.Children)}[/]",
        SpeechStyle.Bold => $"[bold]{Of(styled.Children)}[/]",
        SpeechStyle.Strikethrough => $"[strikethrough]{Of(styled.Children)}[/]",
        _ => throw new NotSupportedException(
            $"No terminal rendering is defined for {styled.Style} emphasis."),
    };
}

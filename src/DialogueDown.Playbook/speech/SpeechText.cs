using System.Collections.Immutable;

namespace DialogueDown.Playbook.Speech;

/// <summary>
/// Reads speech as one line of plain text: the words only, without styling, tags, or commands.
/// </summary>
/// <remarks>
/// For readers that want the words rather than a rendering: a test asserting what was said, a
/// table listing a script's lines, a log naming what just played. A tag describes the line and a
/// command is something the host does, so neither gives any text; a link or an image gives its
/// label or alt text. <c>*Hello*, `"Hero"`.</c> reads as <c>Hello, {Hero}.</c>
/// </remarks>
public static class SpeechText
{
    /// <summary>
    /// Reads <paramref name="speech"/> as one line of plain text, writing each query as its key in
    /// braces.
    /// </summary>
    /// <param name="speech">The speech to read.</param>
    /// <returns>The words, in order. Empty when nothing is said.</returns>
    public static string Of(ImmutableArray<SpeechFragment> speech) => Of(speech, PlaceholderFor);

    /// <summary>
    /// Reads <paramref name="speech"/> as one line of plain text, asking
    /// <paramref name="answerQuery"/> what each query is worth.
    /// </summary>
    /// <param name="speech">The speech to read.</param>
    /// <param name="answerQuery">
    /// Returns the text for a query's key. A caller that knows only some keys can pass the rest to
    /// <see cref="PlaceholderFor"/>.
    /// </param>
    /// <returns>The words, in order. Empty when nothing is said.</returns>
    public static string Of(
        ImmutableArray<SpeechFragment> speech, Func<string, string> answerQuery) =>
        string.Concat(speech.Select(fragment => Of(fragment, answerQuery)));

    /// <summary>Writes a query as its key in braces, for a reader with no value to put there.</summary>
    /// <param name="key">What the query asks the world for.</param>
    /// <returns>The key, in braces.</returns>
    /// <remarks>
    /// The key <c>Hero</c> gives <c>{Hero}</c>: a value belongs here that only a running game
    /// knows. The braces are a display convention, not script syntax, so a brace a writer types in
    /// a line or a key looks the same as one added here.
    /// </remarks>
    public static string PlaceholderFor(string key) => "{" + key + "}";

    private static string Of(SpeechFragment fragment, Func<string, string> answerQuery) =>
        fragment switch
        {
            TextFragment text => text.Text,
            StyledTextFragment styled => Of(styled.Children, answerQuery),
            // A link or an image gives its words, not its address.
            LinkFragment link => Of(link.Label, answerQuery),
            ImageFragment image => Of(image.Alt, answerQuery),
            // A break records where the source wrapped, not a break the writer asked for (that
            // starts a new line). The words on either side belong together, so a space joins them.
            LineBreakFragment => " ",
            QueryFragment query => answerQuery(query.Key),
            _ => string.Empty,
        };
}

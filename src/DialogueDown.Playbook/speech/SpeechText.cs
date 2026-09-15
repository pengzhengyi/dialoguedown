using System.Collections.Immutable;

namespace DialogueDown.Playbook.Speech;

/// <summary>
/// Reads a run of speech as one line of plain text: the words, with the styling, the nesting, and
/// the markup left behind.
/// </summary>
/// <remarks>
/// Speech is a list of fragments because showing it is the host's job, and a browser, a terminal,
/// and a game engine each show it differently. Some readers are not showing it to a player at all,
/// though — a test asserting what was said, a table listing a script's lines, a log naming what
/// just played. They want the words, and they all want the same words, so this is the one place
/// that decides what they are.
///
/// The reading is lossy on purpose and has no way back. Anything that is not a word a speaker says
/// contributes nothing: a tag describes the line rather than belonging to it, and a command is
/// something the host does rather than something anybody says.
/// </remarks>
public static class SpeechText
{
    /// <summary>
    /// Reads <paramref name="speech"/> as one line of plain text, naming each query rather than
    /// saying what it is worth.
    /// </summary>
    /// <param name="speech">The run of speech to read.</param>
    /// <returns>The words, in order, exactly as they compose. Empty when nothing is said.</returns>
    public static string Of(ImmutableArray<SpeechFragment> speech) => Of(speech, PlaceholderFor);

    /// <summary>
    /// Reads <paramref name="speech"/> as one line of plain text, asking
    /// <paramref name="answerQuery"/> what each query is worth.
    /// </summary>
    /// <param name="speech">The run of speech to read.</param>
    /// <param name="answerQuery">
    /// What the world says a query key is worth. A reader that knows some keys and not others can
    /// hand the rest to <see cref="PlaceholderFor"/>.
    /// </param>
    /// <returns>The words, in order, exactly as they compose. Empty when nothing is said.</returns>
    public static string Of(
        ImmutableArray<SpeechFragment> speech, Func<string, string> answerQuery) =>
        string.Concat(speech.Select(fragment => Of(fragment, answerQuery)));

    /// <summary>Writes a query as its key in braces, for a reader with no value to put there.</summary>
    /// <param name="key">What the query asks the world for.</param>
    /// <returns>The key, in braces.</returns>
    /// <remarks>
    /// The braces say what the words alone cannot: a value belongs here, and whoever is reading
    /// cannot know it yet, because only a running game can. They are a reading convention rather
    /// than script syntax, so writing these characters in a script writes the characters.
    ///
    /// A key carries any character but a quote, so one holding a brace of its own reads awkwardly
    /// here, as does prose that happens to contain a brace. Both are rare enough to accept.
    /// </remarks>
    public static string PlaceholderFor(string key) => "{" + key + "}";

    private static string Of(SpeechFragment fragment, Func<string, string> answerQuery) =>
        fragment switch
        {
            TextFragment text => text.Text,
            StyledTextFragment styled => Of(styled.Children, answerQuery),
            // A link and an image each carry words for a reader alongside somewhere to find the
            // real thing. The words are what is being said; the address is not.
            LinkFragment link => Of(link.Label, answerQuery),
            ImageFragment image => Of(image.Alt, answerQuery),
            // A break records where the writer's own source wrapped, not a break they asked for —
            // a break they asked for arrives as the next line instead. So the words either side of
            // it belong to one another, and a space is what keeps them from running together.
            LineBreakFragment => " ",
            QueryFragment query => answerQuery(query.Key),
            _ => string.Empty,
        };
}

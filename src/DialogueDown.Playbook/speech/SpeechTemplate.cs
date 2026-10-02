using System.Collections.Immutable;
using DialogueDown.Playbook.Common;

namespace DialogueDown.Playbook.Speech;

/// <summary>
/// Reads speech as a template whose queries are the placeholders: lists their keys, fills them
/// with answers, and splits the speech into segments at its commands.
/// </summary>
/// <remarks>
/// A line such as <c>Alice: Hello, `"playerName"`.</c> cannot be spoken until the world says what
/// <c>playerName</c> is. <see cref="Keys"/> and <see cref="Fill"/> walk the fragments the same
/// way, so every key that is asked about is a query that gets filled, and the reverse.
/// <para>
/// A query is found wherever it sits: inside emphasis, a link's label, or an image's alt text as
/// well as in plain words. Filling it leaves the emphasis, the link, and the image where the
/// writer put them.
/// </para>
/// </remarks>
public static class SpeechTemplate
{
    /// <summary>Splits speech into the segments a host works through in turn.</summary>
    /// <param name="speech">The speech to break up.</param>
    /// <returns>
    /// The segments, in written order. Always at least one, so speech that says nothing still
    /// reads as a single segment with no words and no command.
    /// </returns>
    /// <remarks>
    /// A command is the boundary. The words before it are said, then it is performed, then the
    /// rest of the line carries on, so a command written mid-sentence is performed mid-sentence.
    /// <c>Alice: Here you go. `GiveQuest("EmberCrown")` Take care.</c> reads as two segments: the
    /// first says <c>Here you go.</c> and gives the quest, the second says <c>Take care.</c>
    /// <para>
    /// Emphasis written around a command divides with it, and is re-applied to the words on each
    /// side, so <c>*polished `Shine()` bright*</c> keeps both halves emphasized. A command inside
    /// a link's label or an image's alt text stays among the words there, because a link and an
    /// image are each one thing and dividing one would make two of it.
    /// </para>
    /// </remarks>
    public static ImmutableArray<SpeechSegment> Segments(ImmutableArray<SpeechFragment> speech) =>
        SegmentBuilder.Of(speech).Freeze();

    /// <summary>The keys the speech's queries ask about, in the order they first appear.</summary>
    /// <param name="speech">The speech to read.</param>
    /// <returns>The keys, each named once.</returns>
    /// <remarks>
    /// Speech naming one key in two places has two queries but one key.
    /// <c>`"Hero"` told `"Hero"`.</c> is read as <c>["Hero"]</c>, and filling puts that single
    /// answer in both places, so the line reads <c>Ada told Ada.</c>
    /// </remarks>
    public static ImmutableArray<string> Keys(ImmutableArray<SpeechFragment> speech)
    {
        var found = new List<string>();
        Rewrite(speech, answer: null, found);

        return [.. found.Distinct(StringComparer.Ordinal)];
    }

    /// <summary>Whether the speech holds a query.</summary>
    /// <param name="speech">The speech to read.</param>
    /// <returns><see langword="true"/> when the speech holds a query, wherever it sits.</returns>
    public static bool HasKeys(ImmutableArray<SpeechFragment> speech) => !Keys(speech).IsEmpty;

    /// <summary>Replaces each query with its answer, leaving everything else as it is.</summary>
    /// <param name="speech">The speech to fill.</param>
    /// <param name="answer">
    /// Returns the answer for a key. Called once per query, so a key used twice is asked twice.
    /// </param>
    /// <returns>The speech, with each query replaced by the words that answered it.</returns>
    /// <remarks>
    /// A query answered with an empty string leaves nothing in its place, and emphasis left with
    /// nothing inside it is removed too. A link or an image stays, with no words to show, because
    /// it is also somewhere to go or a picture to draw.
    /// </remarks>
    public static ImmutableArray<SpeechFragment> Fill(
        ImmutableArray<SpeechFragment> speech, Func<string, string> answer)
    {
        ArgumentNullException.ThrowIfNull(answer);

        return Rewrite(speech, answer, found: []);
    }

    // Serves both Keys and Fill. With no answer it only records the keys and returns the speech
    // unchanged; with one it also replaces each query. Sharing one walk keeps the keys asked about
    // and the queries filled the same set.
    private static ImmutableArray<SpeechFragment> Rewrite(
        ImmutableArray<SpeechFragment> speech, Func<string, string>? answer, List<string> found) =>
        [.. speech.OrEmpty().Select(fragment => Rewrite(fragment, answer, found)).OfType<SpeechFragment>()];

    // Returns null for a fragment left empty, which the caller drops.
    private static SpeechFragment? Rewrite(
        SpeechFragment fragment, Func<string, string>? answer, List<string> found)
    {
        switch (fragment)
        {
            case QueryFragment query:
                found.Add(query.Key);
                return answer is null ? query : Words(answer(query.Key));
            case StyledTextFragment styled:
                return Rewrite(styled.Children, answer, found) is { IsEmpty: false } children
                    ? new StyledTextFragment(styled.Style, children)
                    : null;
            case LinkFragment link:
                return new LinkFragment(link.Target, Rewrite(link.Label, answer, found));
            case ImageFragment image:
                return new ImageFragment(image.Source, Rewrite(image.Alt, answer, found));
            default:
                return fragment;
        }
    }

    // A text fragment cannot be empty, so an empty answer gives no fragment.
    private static TextFragment? Words(string answered) =>
        answered.Length == 0 ? null : new TextFragment(answered);
}

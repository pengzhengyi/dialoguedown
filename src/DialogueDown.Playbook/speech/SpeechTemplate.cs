using System.Collections.Immutable;
using DialogueDown.Playbook.Common;

namespace DialogueDown.Playbook.Speech;

/// <summary>
/// Reads a run of speech as words with holes in it, where the queries are the holes.
/// </summary>
/// <remarks>
/// A line such as <c>Alice: Hello, `"playerName"`.</c> cannot be spoken until the world says what
/// <c>playerName</c> is worth, and two questions follow. Which keys must be asked about, and what
/// does the line say once they are answered? Both are answered here by one walk over the same
/// fragments, so a key that gets asked about is a hole that gets filled, and the reverse.
/// <para>
/// A hole is found wherever it sits. A query inside emphasis, a link's label, or an image's alt
/// text is as much a hole as one in plain prose, and filling it leaves the emphasis, the link, and
/// the image exactly where the writer put them.
/// </para>
/// </remarks>
public static class SpeechTemplate
{
    /// <summary>Breaks a run of speech into the segments a host works through in turn.</summary>
    /// <param name="speech">The speech to break up.</param>
    /// <returns>
    /// The segments, in written order. Always at least one, so speech that says nothing still
    /// reads as a single silent segment.
    /// </returns>
    /// <remarks>
    /// A command is the boundary. The words before it are said, then it is performed, then the
    /// rest of the line carries on, which is what keeps a stage direction written mid-sentence
    /// firing mid-sentence. <c>Alice: Here you go. `GiveQuest("EmberCrown")` Take care.</c> reads
    /// as two segments: the first says <c>Here you go.</c> and gives the quest, the second says
    /// <c>Take care.</c>
    /// <para>
    /// Emphasis written around a command divides with it, and is re-applied to the words on each
    /// side, so <c>*polished `Shine()` bright*</c> keeps both halves emphasized. A command inside
    /// a link's label or an image's alt text stays among the words there, because a link and an
    /// image are each one thing and dividing one would make two of it.
    /// </para>
    /// </remarks>
    public static ImmutableArray<SpeechSegment> Segments(ImmutableArray<SpeechFragment> speech) =>
        SegmentBuilder.Of(speech).Freeze();

    /// <summary>The keys a run of speech asks the world about, in the order they first appear.</summary>
    /// <param name="speech">The speech to read.</param>
    /// <returns>The keys, each named once.</returns>
    /// <remarks>
    /// Speech naming one key in two places has two holes but one key.
    /// <c>`"Hero"` told `"Hero"`.</c> is read as <c>["Hero"]</c>, and filling puts that single
    /// answer in both holes, so the line reads <c>Ada told Ada.</c>
    /// </remarks>
    public static ImmutableArray<string> Keys(ImmutableArray<SpeechFragment> speech)
    {
        var found = new List<string>();
        Rewrite(speech, answer: null, found);

        return [.. found.Distinct(StringComparer.Ordinal)];
    }

    /// <summary>Fills every hole with what the world said, and leaves the rest standing.</summary>
    /// <param name="speech">The speech to fill.</param>
    /// <param name="answer">What a key is worth. Asked once per hole, repeats included.</param>
    /// <returns>The speech, with each query replaced by the words that answered it.</returns>
    public static ImmutableArray<SpeechFragment> Fill(
        ImmutableArray<SpeechFragment> speech, Func<string, string> answer)
    {
        ArgumentNullException.ThrowIfNull(answer);

        return Rewrite(speech, answer, found: []);
    }

    // One walk, two uses. With no answer it only takes note of the keys and leaves the speech as it
    // found it; with one it puts the words in their place. Writing it once is what stops the keys
    // that get asked about and the holes that get filled from ever being two different sets.
    private static ImmutableArray<SpeechFragment> Rewrite(
        ImmutableArray<SpeechFragment> speech, Func<string, string>? answer, List<string> found) =>
        [.. speech.OrEmpty().Select(fragment => Rewrite(fragment, answer, found))];

    private static SpeechFragment Rewrite(
        SpeechFragment fragment, Func<string, string>? answer, List<string> found)
    {
        switch (fragment)
        {
            case QueryFragment query:
                found.Add(query.Key);
                return answer is null ? query : new TextFragment(answer(query.Key));
            case StyledTextFragment styled:
                return new StyledTextFragment(styled.Style, Rewrite(styled.Children, answer, found));
            case LinkFragment link:
                return new LinkFragment(link.Target, Rewrite(link.Label, answer, found));
            case ImageFragment image:
                return new ImageFragment(image.Source, Rewrite(image.Alt, answer, found));
            default:
                return fragment;
        }
    }
}

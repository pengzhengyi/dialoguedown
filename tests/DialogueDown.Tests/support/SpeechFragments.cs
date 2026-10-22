using DialogueDown.Playbook.Speech;

namespace DialogueDown.Tests.Support;

/// <summary>
/// Every fragment of a speech list, including those that other fragments hold.
/// </summary>
/// <remarks>
/// Each fragment comes before the fragments it holds, in the order they are written. The speech of
/// <code>
/// Take **the [old ![rusty](key.png)](shed) key**.
/// </code>
/// lists <c>Take </c>, the bold text, <c>the </c>, the link, <c>old </c>, the image, <c>rusty</c>,
/// <c> key</c>, and <c>.</c>. Styled text and a link's label belong to the list that holds them,
/// but an image's alt text is a list of its own, which is spaced separately. <see cref="All"/>
/// walks into it, and <see cref="AllButAltText"/> does not, so it leaves out <c>rusty</c>.
/// </remarks>
internal static class SpeechFragments
{
    /// <summary>Every fragment in a speech list, including those in an image's alt text.</summary>
    /// <param name="speech">The speech list.</param>
    /// <returns>Each fragment, before the fragments it holds.</returns>
    public static IEnumerable<SpeechFragment> All(IEnumerable<SpeechFragment> speech) =>
        Walk(speech, Held);

    /// <summary>
    /// Every fragment in a speech list, but not those in an image's alt text, which is a list of
    /// its own.
    /// </summary>
    /// <param name="speech">The speech list.</param>
    /// <returns>Each fragment, before the fragments it holds.</returns>
    public static IEnumerable<SpeechFragment> AllButAltText(IEnumerable<SpeechFragment> speech) =>
        Walk(speech, fragment => fragment is ImageFragment ? [] : Held(fragment));

    // Each fragment, then the walk of what it holds.
    private static IEnumerable<SpeechFragment> Walk(
        IEnumerable<SpeechFragment> speech,
        Func<SpeechFragment, IEnumerable<SpeechFragment>> held) =>
        speech.SelectMany(fragment => Walk(held(fragment), held).Prepend(fragment));

    // Every kind is named, so a kind added later that holds speech fails here rather than hiding
    // what it holds.
    private static IEnumerable<SpeechFragment> Held(SpeechFragment fragment) =>
        fragment switch
        {
            StyledTextFragment styled => styled.Children,
            LinkFragment link => link.Label,
            ImageFragment image => image.Alt,
            TextFragment or LineBreakFragment or QueryFragment or TagFragment
                or DefaultCommandFragment or CustomCommandFragment => [],
            _ => throw new NotSupportedException(
                $"{fragment.GetType().Name} is new to this walk: say what speech it holds."),
        };
}

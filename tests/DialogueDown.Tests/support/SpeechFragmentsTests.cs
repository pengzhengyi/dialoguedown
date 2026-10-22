using DialogueDown.Playbook.Speech;
using static DialogueDown.TestSupport.SpeechFactory;

namespace DialogueDown.Tests.Support;

public sealed class SpeechFragmentsTests
{
    [Fact]
    public void All_ListsEachFragmentBeforeThoseItHolds_AltTextIncluded() =>
        Assert.Equal(
            ["Take ", "<styled>", "the ", "<link>", "old ", "<image>", "rusty", " key", "."],
            SpeechFragments.All(TakeTheOldRustyKey()).Select(Named));

    [Fact]
    public void AllButAltText_LeavesOutAnImagesAltText() =>
        Assert.Equal(
            ["Take ", "<styled>", "the ", "<link>", "old ", "<image>", " key", "."],
            SpeechFragments.AllButAltText(TakeTheOldRustyKey()).Select(Named));

    /// <summary>
    /// The speech of a line with a link inside bold text, and an image inside the link.
    /// </summary>
    /// <remarks>
    /// <code>
    /// Take **the [old ![rusty](key.png)](shed) key**.
    /// </code>
    /// </remarks>
    private static SpeechFragment[] TakeTheOldRustyKey() =>
        [
            Text("Take "),
            Bold(
                Text("the "),
                Link("shed", Text("old "), Image("key.png", Text("rusty"))),
                Text(" key")),
            Text("."),
        ];

    // A text by its words, and any other fragment by its kind in angle brackets.
    private static string Named(SpeechFragment fragment) =>
        fragment switch
        {
            TextFragment text => text.Text,
            StyledTextFragment => "<styled>",
            LinkFragment => "<link>",
            ImageFragment => "<image>",
            _ => $"<{fragment.GetType().Name}>",
        };
}

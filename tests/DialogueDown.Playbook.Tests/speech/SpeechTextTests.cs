using DialogueDown.Playbook.Speech;
using static DialogueDown.Playbook.Tests.Support.PlaybookFactory;

namespace DialogueDown.Playbook.Tests.Speech;

public sealed class SpeechTextTests
{
    [Fact]
    public void Of_WithoutFragments_SaysNothing()
    {
        Assert.Equal(string.Empty, SpeechText.Of([]));
    }

    [Fact]
    public void Of_ConcatenatesPlainText()
    {
        Assert.Equal("My key is rusty.", SpeechText.Of([Text("My key is "), Text("rusty.")]));
    }

    [Fact]
    public void Of_KeepsAStyledRunsWordsAndDropsItsStyle()
    {
        Assert.Equal(
            "My key is rusty.",
            SpeechText.Of([Text("My key is "), Bold("rusty"), Text(".")]));
    }

    [Fact]
    public void Of_FollowsStylingNestedInsideStyling()
    {
        Assert.Equal("very rusty", SpeechText.Of([Bold(Text("very "), Italic("rusty"))]));
    }

    [Fact]
    public void Of_ReadsALinksLabelRatherThanItsTarget()
    {
        Assert.Equal(
            "the old map",
            SpeechText.Of([Link("https://example.test/map", Text("the old map"))]));
    }

    [Fact]
    public void Of_ReadsAnImagesAltRatherThanItsSource()
    {
        Assert.Equal("a rusted key", SpeechText.Of([Image("art/key.png", Text("a rusted key"))]));
    }

    [Fact]
    public void Of_RendersALineBreakAsASpace()
    {
        // A break in speech is where the writer's source wrapped, not a break they asked for, so
        // the words either side of it belong to one another with a space between.
        Assert.Equal("one two", SpeechText.Of([Text("one"), LineBreak(), Text("two")]));
    }

    [Fact]
    public void Of_SaysNothingForATagOrACommand()
    {
        // A tag is something a host reads about the line and a command is something it performs.
        // Neither is a word the speaker says, so neither belongs in the words.
        Assert.Equal(
            "Careful.",
            SpeechText.Of(
            [
                Tag("wary"),
                Text("Careful."),
                DefaultCommand("fade out"),
                CustomCommand("ShowSprite", "yuki", "urgent"),
            ]));
    }

    [Fact]
    public void Of_KeepsTheSpaceAroundTheWords()
    {
        // Speech often opens or closes on a space that matters once it sits beside another
        // fragment. Trimming is the caller's decision, not this one's.
        Assert.Equal(" spaced ", SpeechText.Of([Text(" spaced ")]));
    }
}

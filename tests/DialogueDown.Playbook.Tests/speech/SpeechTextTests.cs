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
    public void Of_WithoutAnswers_NamesTheQueryInBraces()
    {
        // Braces mark the one part of the line this reader cannot know. They are a drawing
        // convention rather than anything a writer types.
        Assert.Equal(
            "You are {HeroName}.",
            SpeechText.Of([Text("You are "), Query("HeroName"), Text(".")]));
    }

    [Fact]
    public void Of_WithAnAnswer_SaysTheAnswer()
    {
        Assert.Equal(
            "You are Ada.",
            SpeechText.Of([Text("You are "), Query("HeroName"), Text(".")], _ => "Ada"));
    }

    [Fact]
    public void Of_WithAnswersForSomeKeys_NamesOnlyTheRest()
    {
        // A reader that knows some of the world hands the keys it does not know to the placeholder,
        // so one unknown key costs only that key.
        Assert.Equal(
            "Ada holds {Gold} gold.",
            SpeechText.Of(
                [Query("HeroName"), Text(" holds "), Query("Gold"), Text(" gold.")],
                key => key == "HeroName" ? "Ada" : SpeechText.PlaceholderFor(key)));
    }

    [Fact]
    public void Of_WithAnAnswerOfItsOwnChoosing_SaysThat()
    {
        // The placeholder is the default rather than the rule: a reader that would rather mark an
        // unknown key its own way says so.
        Assert.Equal(
            "You are ???.",
            SpeechText.Of([Text("You are "), Query("HeroName"), Text(".")], _ => "???"));
    }

    [Fact]
    public void PlaceholderFor_WrapsTheKeyInBraces()
    {
        Assert.Equal("{HeroName}", SpeechText.PlaceholderFor("HeroName"));
    }

    [Fact]
    public void Of_WithAnEmptyAnswer_SaysNothingForTheQuery()
    {
        // An answered query is answered. A world that says a key is worth nothing has still said
        // so, and speaking for it would overrule the answer.
        Assert.Equal(
            "You are .",
            SpeechText.Of([Text("You are "), Query("HeroName"), Text(".")], _ => string.Empty));
    }

    [Fact]
    public void Of_ReadsAQueryNestedInsideStyling()
    {
        Assert.Equal("Ada", SpeechText.Of([Bold(Query("HeroName"))], _ => "Ada"));
    }

    [Fact]
    public void Of_KeepsTheSpaceAroundTheWords()
    {
        // Speech often opens or closes on a space that matters once it sits beside another
        // fragment. Trimming is the caller's decision, not this one's.
        Assert.Equal(" spaced ", SpeechText.Of([Text(" spaced ")]));
    }
}

using DialogueDown.Script.Ast;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Ast;

public sealed class InlineTextTests
{
    [Fact]
    public void Of_ConcatenatesPlainText()
    {
        Assert.Equal("Hello world", InlineText.Of([Text("Hello "), Text("world")]));
    }

    [Fact]
    public void Of_FlattensStyledAndLinkChildren()
    {
        Assert.Equal(
            "boldhere",
            InlineText.Of(
                [StyledText(SpeechStyle.Italic, Text("bold")), Link("#x", Text("here"))]));
    }

    // A jump's label and an image's alt are the words a reader sees, so a flattened run keeps
    // them rather than dropping to an empty string where they appear.
    [Fact]
    public void Of_ReadsAJumpsLabelAndAnImagesAlt()
    {
        Assert.Equal(
            "go therea map",
            InlineText.Of([Jump("#scene", Text("go there")), Image("map.png", Text("a map"))]));
    }

    [Fact]
    public void Of_RendersALineBreakAsASpace()
    {
        Assert.Equal("a b", InlineText.Of([Text("a"), LineBreak(), Text("b")]));
    }

    // A query's value is only knowable while a game runs, so a reading with no game behind it
    // names the query rather than leaving a gap where its words would have stood.
    [Fact]
    public void Of_NamesAQueryItCannotAnswer()
    {
        Assert.Equal(
            "You are {HeroName}.",
            InlineText.Of([Text("You are "), Query("HeroName"), Text(".")]));
    }

    [Fact]
    public void Of_SaysNothingForACommand()
    {
        Assert.Equal(
            "Careful.", InlineText.Of([DefaultCommand("fade out"), Text("Careful.")]));
    }
}

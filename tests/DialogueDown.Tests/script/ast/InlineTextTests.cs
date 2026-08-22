using DialogueDown.Common;
using DialogueDown.Script.Ast;

namespace DialogueDown.Tests.Script.Ast;

public sealed class InlineTextTests
{
    [Fact]
    public void Of_ConcatenatesPlainText()
    {
        var fragments = new InlineFragment[] { new Text("Hello ", Span()), new Text("world", Span()) };
        Assert.Equal("Hello world", InlineText.Of(fragments));
    }

    [Fact]
    public void Of_FlattensStyledAndLinkChildren()
    {
        var styled = new StyledText(SpeechStyle.Italic, [new Text("bold", Span())], Span());
        var link = new Link("#x", [new Text("here", Span())], Span());
        Assert.Equal("boldhere", InlineText.Of([styled, link]));
    }

    // A jump's label and an image's alt are the words a reader sees, so a flattened run keeps
    // them rather than dropping to an empty string where they appear.
    [Fact]
    public void Of_ReadsAJumpsLabelAndAnImagesAlt()
    {
        var jump = new Jump("#scene", [new Text("go there", Span())], Span());
        var image = new Image("map.png", [new Text("a map", Span())], Span());

        Assert.Equal("go therea map", InlineText.Of([jump, image]));
    }

    [Fact]
    public void Of_RendersALineBreakAsASpace()
    {
        var fragments = new InlineFragment[] { new Text("a", Span()), new LineBreak(Span()), new Text("b", Span()) };
        Assert.Equal("a b", InlineText.Of(fragments));
    }

    private static SourceSpan Span() => new(0, 1);
}

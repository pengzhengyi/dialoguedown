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

    [Fact]
    public void Of_RendersALineBreakAsASpace()
    {
        var fragments = new InlineFragment[] { new Text("a", Span()), new LineBreak(Span()), new Text("b", Span()) };
        Assert.Equal("a b", InlineText.Of(fragments));
    }

    private static SourceSpan Span() => new(0, 1);
}

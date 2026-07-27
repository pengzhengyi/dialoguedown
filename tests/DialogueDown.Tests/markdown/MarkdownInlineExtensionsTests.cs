using DialogueDown.Common;
using DialogueDown.Markdown;
using static DialogueDown.Tests.Support.MarkdownAstFactory;

namespace DialogueDown.Tests.Markdown;

public sealed class MarkdownInlineExtensionsTests
{
    [Fact]
    public void TrimLeadingWhitespace_Text_TrimsAndReanchorsItsSpan()
    {
        var text = new TextInline("  Alice: Hi", new SourceSpan(4, 11));

        var trimmed = text.TrimLeadingWhitespace();

        Assert.NotNull(trimmed);
        Assert.Equal("Alice: Hi", trimmed.Text);
        Assert.Equal(new SourceSpan(6, 9), trimmed.Span);
    }

    [Fact]
    public void TrimLeadingWhitespace_Text_ReturnsTheSameInline_WhenItHasNoLeadingWhitespace()
    {
        var text = new TextInline("Alice", new SourceSpan(0, 5));

        Assert.Same(text, text.TrimLeadingWhitespace());
    }

    [Fact]
    public void TrimLeadingWhitespace_Text_ReturnsNull_WhenEntirelyWhitespace()
    {
        var text = new TextInline("   ", new SourceSpan(0, 3));

        Assert.Null(text.TrimLeadingWhitespace());
    }

    [Fact]
    public void TrimLeadingWhitespace_TrimsTheFirstTextInline_AndReanchorsItsSpan()
    {
        var text = new TextInline("  Alice: Hi", new SourceSpan(4, 11));
        IReadOnlyList<MarkdownInline> inlines = [text];

        var head = Assert.IsType<TextInline>(Assert.Single(inlines.TrimLeadingWhitespace()));

        Assert.Equal("Alice: Hi", head.Text);
        Assert.Equal(new SourceSpan(6, 9), head.Span);
    }

    [Fact]
    public void TrimLeadingWhitespace_DropsAWhitespaceOnlyLeadingText()
    {
        var space = new TextInline("  ", new SourceSpan(0, 2));
        var next = CodeSpan("50%");
        IReadOnlyList<MarkdownInline> inlines = [space, next];

        Assert.Same(next, Assert.Single(inlines.TrimLeadingWhitespace()));
    }

    [Fact]
    public void TrimLeadingWhitespace_IsUnchanged_WhenTheFirstTextHasNoLeadingWhitespace()
    {
        IReadOnlyList<MarkdownInline> inlines = [new TextInline("Alice", Span())];

        Assert.Same(inlines, inlines.TrimLeadingWhitespace());
    }

    [Fact]
    public void TrimLeadingWhitespace_IsUnchanged_WhenTheFirstInlineIsNotText()
    {
        IReadOnlyList<MarkdownInline> inlines = [CodeSpan("50%"), Text(" tail")];

        Assert.Same(inlines, inlines.TrimLeadingWhitespace());
    }

    [Fact]
    public void TrimLeadingWhitespace_Empty_IsEmpty() =>
        Assert.Empty(Array.Empty<MarkdownInline>().TrimLeadingWhitespace());

    [Fact]
    public void PlainText_Text_IsItsText() =>
        Assert.Equal("Alice", Text("Alice").PlainText());

    [Fact]
    public void PlainText_Emphasis_DropsTheStylingAndKeepsTheWords() =>
        Assert.Equal("Alice", Emphasis(EmphasisKind.Italic, Text("Alice")).PlainText());

    [Fact]
    public void PlainText_NestedEmphasis_Flattens() =>
        Assert.Equal(
            "Al", Emphasis(EmphasisKind.Bold, Emphasis(EmphasisKind.Italic, Text("Al"))).PlainText());

    [Fact]
    public void PlainText_ACodeSpan_IsNull() => Assert.Null(CodeSpan("x").PlainText());

    [Fact]
    public void PlainText_ALink_IsNull() => Assert.Null(Link("t", Text("x")).PlainText());

    [Fact]
    public void PlainText_AnImage_IsNull() => Assert.Null(Image("s", Text("x")).PlainText());

    [Fact]
    public void PlainText_ALineBreak_IsNull() => Assert.Null(LineBreak().PlainText());

    [Fact]
    public void PlainText_ARun_ConcatenatesEachInline() =>
        Assert.Equal(
            "Alice",
            new MarkdownInline[] { Text("A"), Emphasis(EmphasisKind.Italic, Text("lice")) }.PlainText());

    [Fact]
    public void PlainText_ARunWithAFunctionalInline_IsNull() =>
        Assert.Null(new MarkdownInline[] { Text("A"), CodeSpan("x") }.PlainText());

    [Fact]
    public void PlainText_AnEmptyRun_IsEmpty() =>
        Assert.Equal("", Array.Empty<MarkdownInline>().PlainText());
}

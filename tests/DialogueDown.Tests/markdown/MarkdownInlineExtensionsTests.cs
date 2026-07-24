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
}

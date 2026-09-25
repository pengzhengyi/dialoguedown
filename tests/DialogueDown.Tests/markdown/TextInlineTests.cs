using DialogueDown.Common;
using DialogueDown.Markdown;
using Ast = DialogueDown.Tests.Support.MarkdownAstFactory;

namespace DialogueDown.Tests.Markdown;

public sealed class TextInlineTests
{
    [Fact]
    public void Constructor_ExposesTextAndSpan_AndIsInline()
    {
        var span = Ast.Span();

        var inline = new TextInline("hello", span);

        Assert.Equal("hello", inline.Text);
        Assert.Equal(span, inline.Span);
        Assert.False(inline.IsFirstCharacterEscaped);
        Assert.IsAssignableFrom<MarkdownInline>(inline);
    }

    [Fact]
    public void Constructor_ContentSpan_DefaultsToSpan()
    {
        var span = new SourceSpan(2, 4);

        var inline = new TextInline("* b", span);

        Assert.Equal(span, inline.ContentSpan);
    }

    [Fact]
    public void Constructor_ContentSpan_IsExposedWhenGivenDistinctly()
    {
        // An escaped literal: the raw span covers the backslash, the content span does not.
        var inline = new TextInline("* b", new SourceSpan(2, 4), new SourceSpan(3, 3));

        Assert.Equal(new SourceSpan(2, 4), inline.Span);
        Assert.Equal(new SourceSpan(3, 3), inline.ContentSpan);
    }

    [Fact]
    public void Constructor_EscapedFirstCharacter_IsExposedWhenSet()
    {
        var inline = new TextInline(
            "* b", new SourceSpan(2, 4), new SourceSpan(3, 3), isFirstCharacterEscaped: true);

        Assert.True(inline.IsFirstCharacterEscaped);
    }

    [Fact]
    public void Constructor_NullText_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new TextInline(null!, Ast.Span()));
    }

    [Fact]
    public void Constructor_EmptyText_Throws()
    {
        Assert.Throws<ArgumentException>(() => new TextInline(string.Empty, Ast.Span()));
    }

    [Fact]
    public void Equality_SameTextAndSpan_AreEqual()
    {
        var span = Ast.Span();

        Assert.Equal(new TextInline("hi", span), new TextInline("hi", span));
    }
}

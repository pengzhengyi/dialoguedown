using DialogueDown.Markdown;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.MarkdownAstAssert;

namespace DialogueDown.Tests.Markdown;

public sealed class MarkdigMarkdownParserEscapeTests : MarkdigMarkdownParserTestBase
{
    [Fact]
    public void Parse_EscapedPunctuation_UnescapesTheTextButKeepsTheRawSpan()
    {
        // "\* b" — the backslash escapes the star; the text is "* b" without it.
        var document = Parse(@"\* b");

        var text = AssertSingleTextInline(document);
        Assert.Equal("* b", text.Text);

        // Span covers the backslash (the raw source); ContentSpan starts past it.
        Assert.Equal(0, text.Span.Start);
        Assert.Equal(4, text.Span.End);
        Assert.Equal(1, text.ContentSpan.Start);
        Assert.Equal(4, text.ContentSpan.End);
    }

    [Fact]
    public void Parse_EscapeInTheMiddle_AnchorsTheSecondRunPastTheBackslash()
    {
        // "ab\*cd" — Markdig splits at the escape into "ab" and "*cd".
        var document = Parse(@"ab\*cd");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        Assert.Equal(2, paragraph.Inlines.Count);

        var second = Assert.IsType<TextInline>(paragraph.Inlines[1]);
        Assert.Equal("*cd", second.Text);
        Assert.Equal(2, second.Span.Start); // raw span still counts the backslash
        Assert.Equal(3, second.ContentSpan.Start); // content anchors at the star
    }

    [Fact]
    public void Parse_PlainText_ContentSpanEqualsSpan()
    {
        var document = Parse("plain");

        var text = AssertSingleTextInline(document);
        Assert.Equal(text.Span, text.ContentSpan);
    }

    private static TextInline AssertSingleTextInline(MarkdownDocument document)
    {
        var paragraph = AssertSingleBlock<Paragraph>(document);
        return Assert.IsType<TextInline>(Assert.Single(paragraph.Inlines));
    }
}

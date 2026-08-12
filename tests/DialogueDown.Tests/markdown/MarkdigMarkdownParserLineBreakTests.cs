using DialogueDown.Markdown;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.MarkdownAstAssert;

namespace DialogueDown.Tests.Markdown;

public sealed class MarkdigMarkdownParserLineBreakTests : MarkdigMarkdownParserTestBase
{
    [Theory]
    [InlineData("line one\nline two")]
    [InlineData("line one\r\nline two")]
    public void Parse_SoftLineBreak_PreservedAsSoftBreak(string source)
    {
        // A plain newline (either line-ending style) is a soft break: the two
        // lines stay in one paragraph and the compiler later joins them.
        var document = Parse(source);

        var paragraph = AssertSingleBlock<Paragraph>(document);
        Assert.Collection(
            paragraph.Inlines,
            inline => AssertText(inline, "line one"),
            inline => AssertLineBreak(inline, isHard: false),
            inline => AssertText(inline, "line two"));
    }

    [Theory]
    [InlineData("line one  \nline two")]
    [InlineData("line one\\\nline two")]
    public void Parse_HardLineBreak_PreservedAsHardBreak(string source)
    {
        // Two trailing spaces or a trailing backslash both make a hard break,
        // which the compiler later reads as the boundary between two speeches.
        var document = Parse(source);

        var paragraph = AssertSingleBlock<Paragraph>(document);
        Assert.Collection(
            paragraph.Inlines,
            inline => AssertText(inline, "line one"),
            inline => AssertLineBreak(inline, isHard: true),
            inline => AssertText(inline, "line two"));
    }

    [Fact]
    public void Parse_SoftWrappedParagraph_AnchorsTheFirstLiteralAtItsAbsolutePosition()
    {
        // Markdig rebuilds a soft-wrapped paragraph's inline content into its own buffer, so a
        // LiteralInline's Content.Start is relative to that buffer (0), not the source. The
        // ContentSpan must still be the absolute source position (taken from the reliable
        // Span), or a speaker or tokenizer anchored at ContentSpan lands at the top of the file.
        var document = Parse("# Heading\n\nAlice: the first line\nsoftwraps onto a second.");

        var paragraph = document.Blocks.OfType<Paragraph>().Single();
        var firstText = paragraph.Inlines.OfType<TextInline>().First();
        Assert.Equal("Alice: the first line", firstText.Text);
        // Plain text has no leading escape, so the content anchors exactly at the raw span —
        // and well past the top of the document, where the buffer-relative offset would land.
        Assert.Equal(firstText.Span.Start, firstText.ContentSpan.Start);
        Assert.NotEqual(0, firstText.ContentSpan.Start);
    }
}

using DialogueDown.Common;
using DialogueDown.Markdown;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.MarkdownAstAssert;

namespace DialogueDown.Tests.Markdown;

public sealed class MarkdigMarkdownParserHeadingTests : MarkdigMarkdownParserTestBase
{
    [Theory]
    [InlineData("# One", 1, "One")]
    [InlineData("###### Six", 6, "Six")]
    public void Parse_Heading_ProducesHeadingWithLevelAndText(string source, int level, string expected)
    {
        var document = Parse(source);

        var heading = AssertSingleBlock<Heading>(document);
        Assert.Equal(level, heading.Level);
        AssertSingleText(heading.Inlines, expected);
    }

    [Theory]
    [InlineData("##     Title")]
    [InlineData("##   Title   ")]
    public void Parse_HeadingWithSurroundingSpaces_KeepsOnlyContent(string source)
    {
        // CommonMark trims the spaces around heading content, so the text is clean.
        var document = Parse(source);

        var heading = AssertSingleBlock<Heading>(document);
        AssertSingleText(heading.Inlines, "Title");
    }

    [Fact]
    public void Parse_EmptyHeading_HasNoInlinesAndSpansTheHashes()
    {
        var document = Parse("##");

        var heading = AssertSingleBlock<Heading>(document);
        Assert.Equal(2, heading.Level);
        Assert.Empty(heading.Inlines);
        Assert.Equal(new SourceSpan(0, 2), heading.Span);
    }

    [Fact]
    public void Parse_HashNotAtLineStart_IsLiteralText()
    {
        // Only a leading '#' starts a heading; a '#' inside a line stays literal.
        var document = Parse("Alice #tag");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        AssertSingleText(paragraph.Inlines, "Alice #tag");
    }
}

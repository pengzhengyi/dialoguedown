using DialogueDown.Markdown;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.MarkdownAstAssert;

namespace DialogueDown.Tests.Markdown;

public sealed class MarkdigMarkdownParserEntityTests : MarkdigMarkdownParserTestBase
{
    [Theory]
    [InlineData("&nbsp;", "\u00A0")]
    [InlineData("&ensp;", "\u2002")]
    [InlineData("&emsp;", "\u2003")]
    [InlineData("&thinsp;", "\u2009")]
    [InlineData("&#32;", " ")]
    [InlineData("&amp;", "&")]
    public void Parse_EntityReference_DecodesToTheCharacterItNames(string entity, string character)
    {
        // CommonMark reads a valid entity as the character it names, so the AST carries that
        // character rather than the source characters that spell it.
        var document = Parse($"a{entity}b");

        Assert.Equal($"a{character}b", TextOf(AssertSingleBlock<Paragraph>(document)));
    }

    [Fact]
    public void Parse_EntityReference_KeepsTheRawSpanOfTheSource()
    {
        var document = Parse("a&nbsp;b");

        var paragraph = AssertSingleBlock<Paragraph>(document);

        var entity = paragraph.Inlines.OfType<TextInline>().Single(text => text.Text == "\u00A0");
        Assert.Equal(1, entity.Span.Start);
        Assert.Equal(7, entity.Span.End);
    }

    [Fact]
    public void Parse_UnknownEntity_StaysLiteralText()
    {
        // Markdig hands an unknown name through as text, so there is nothing to decode and
        // nothing to report.
        var document = Parse("a&notanentity;b");

        Assert.Equal("a&notanentity;b", TextOf(AssertSingleBlock<Paragraph>(document)));
    }

    private static string TextOf(Paragraph paragraph) =>
        string.Concat(paragraph.Inlines.OfType<TextInline>().Select(text => text.Text));
}

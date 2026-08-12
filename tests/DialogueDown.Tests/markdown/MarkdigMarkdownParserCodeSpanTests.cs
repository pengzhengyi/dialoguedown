using DialogueDown.Markdown;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.MarkdownAstAssert;

namespace DialogueDown.Tests.Markdown;

public sealed class MarkdigMarkdownParserCodeSpanTests : MarkdigMarkdownParserTestBase
{
    [Fact]
    public void Parse_CodeSpan_ProducesCodeSpanInlineWithRawContent()
    {
        var document = Parse("`\"Alice.FavoriteColor\"`");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        var code = AssertSingleInline<CodeSpanInline>(paragraph.Inlines);
        Assert.Equal("\"Alice.FavoriteColor\"", code.Content);
    }

    [Fact]
    public void Parse_CodeSpanWithMarkers_KeepsRawContent()
    {
        // Anything inside a code span is verbatim, including would-be styling markers.
        var document = Parse("`it *stays* raw`");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        var code = AssertSingleInline<CodeSpanInline>(paragraph.Inlines);
        Assert.Equal("it *stays* raw", code.Content);
    }

    [Fact]
    public void Parse_WhitespaceOnlyCodeSpan_IsAccepted()
    {
        // Syntactically valid; an empty/whitespace command is rejected later, not here.
        var document = Parse("` `");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        var code = AssertSingleInline<CodeSpanInline>(paragraph.Inlines);
        Assert.Equal(" ", code.Content);
    }
}

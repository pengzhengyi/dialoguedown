using DialogueDown.Markdown;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.MarkdownAstAssert;

namespace DialogueDown.Tests.Markdown;

public sealed class MarkdigMarkdownParserUnmodeledContentTests : MarkdigMarkdownParserTestBase
{
    private const string CodeBlockSource =
        """
        ```mermaid
        graph TD
        A --> B
        ```
        """;

    private const string TableSource =
        """
        | Speaker | Mood  |
        | ------- | ----- |
        | Alice   | happy |
        """;

    [Theory]
    [InlineData("---")]
    [InlineData(CodeBlockSource)]
    [InlineData(TableSource)]
    public void Parse_IgnoredByDefault_ProducesEmptyDocument(string source)
    {
        // Authoring aids (dividers, code/diagrams, tables) are not speech, so the
        // default policy drops them.
        var document = Parse(source);

        Assert.Empty(document.Blocks);
    }

    [Theory]
    [InlineData("<div>hi</div>")]   // raw HTML block
    public void Parse_RawTextByDefault_FlattensToParagraph(string source)
    {
        // Ambiguous constructs may be intended content, so the default policy keeps
        // them as a paragraph of their exact source text.
        var document = Parse(source);

        var paragraph = AssertSingleBlock<Paragraph>(document);
        AssertSingleText(paragraph.Inlines, source);
    }

    [Fact]
    public void Parse_Blockquote_ModelsAQuoteBlockWrappingItsContent()
    {
        // A blockquote is kept structurally — a wrapper around its inner blocks — so a
        // marker-headed quote can later be recognized as a block conditional.
        var document = Parse("> quote");

        var quote = AssertSingleBlock<QuoteBlock>(document);
        var paragraph = Assert.IsType<Paragraph>(Assert.Single(quote.Blocks));
        AssertSingleText(paragraph.Inlines, "quote");
    }

    [Theory]
    [InlineData("<https://example.com>")]
    [InlineData("<mailto:alice@example.com>")]
    public void Parse_UnmodeledInline_FlattensToRawText(string source)
    {
        // Autolinks are kept as their exact source text by default.
        var document = Parse(source);

        var paragraph = AssertSingleBlock<Paragraph>(document);
        AssertSingleText(paragraph.Inlines, source);
    }

    [Fact]
    public void Parse_RawInlineHtml_FlattensToRawText()
    {
        // Inline HTML is kept as raw text (each tag flattens; surrounding text stays).
        var document = Parse("<b>hi</b>");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        AssertAllText(paragraph.Inlines, "<b>hi</b>");
    }
}

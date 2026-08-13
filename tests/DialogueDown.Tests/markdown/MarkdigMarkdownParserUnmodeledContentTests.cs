using DialogueDown.Diagnostics;
using DialogueDown.Markdown;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DiagnosticsAssert;
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

    [Theory]
    [InlineData("---", "divider")]
    [InlineData(CodeBlockSource, "code block")]
    [InlineData(TableSource, "table")]
    public void Parse_ADroppedConstruct_IsNotedWithItsKind(string source, string kind)
    {
        Parse(source, out var diagnostics);

        var note = AssertReported(diagnostics.Diagnostics, DiagnosticCatalog.IgnoredUnmodeledMarkdown);
        Assert.Equal(DiagnosticSeverity.Info, note.Severity);
        Assert.Equal(kind, Assert.Single(note.MessageArguments));
    }

    [Fact]
    public void Parse_ADroppedConstruct_PointsAtTheConstruct()
    {
        var source =
            $"""
            Alice: Hi

            {TableSource}
            """;

        Parse(source, out var diagnostics);

        var note = AssertReported(diagnostics.Diagnostics, DiagnosticCatalog.IgnoredUnmodeledMarkdown);
        Assert.Equal(source.IndexOf("| Speaker", StringComparison.Ordinal), note.Span.Start);
    }

    [Fact]
    public void Parse_AKeptConstruct_IsNotNoted()
    {
        Parse("<div>hi</div>", out var diagnostics);

        AssertNotReported(diagnostics.Diagnostics);
    }

    [Fact]
    public void Parse_SeveralDroppedConstructs_AreEachNoted()
    {
        // The dividers follow content, so they are ordinary thematic breaks rather than the front
        // matter a leading "---" would open.
        Parse(
            """
            Alice: Hi

            ---

            Bob: Bye

            ---
            """,
            out var diagnostics);

        Assert.Equal(2, diagnostics.Diagnostics.Count);
    }

    [Fact]
    public void Parse_ADropInsideAListItem_IsNoted()
    {
        // The converter recurses into nested blocks, so a drop there is reported too.
        Parse(
            """
            - Alice: Hi

                ```mermaid
                graph TD
                ```
            """,
            out var diagnostics);

        Assert.Equal(
            "code block",
            Assert.Single(
                AssertReported(diagnostics.Diagnostics, DiagnosticCatalog.IgnoredUnmodeledMarkdown)
                    .MessageArguments));
    }
}

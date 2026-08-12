using DialogueDown.Markdown;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.MarkdownAstAssert;

namespace DialogueDown.Tests.Markdown;

public sealed class MarkdigMarkdownParserParagraphTests : MarkdigMarkdownParserTestBase
{
    [Fact]
    public void Parse_PlainParagraph_ProducesParagraphWithRawText()
    {
        var document = Parse("Hello, Bob!");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        AssertSingleText(paragraph.Inlines, "Hello, Bob!");
    }
}

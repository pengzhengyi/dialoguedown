using DialogueDown.Markdown;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.MarkdownAstAssert;

namespace DialogueDown.Tests.Markdown;

public sealed class MarkdigMarkdownParserImageTests : MarkdigMarkdownParserTestBase
{
    [Fact]
    public void Parse_Image_ProducesImageInlineWithSourceAndAlt()
    {
        var document = Parse("![Play tennis](tennis.png)");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        var image = AssertSingleInline<ImageInline>(paragraph.Inlines);
        Assert.Equal("tennis.png", image.Source);
        AssertSingleText(image.Alt, "Play tennis");
    }

    [Fact]
    public void Parse_ImageWithEmptySource_IsAccepted()
    {
        // Syntactically valid; a missing image source is rejected later, not here.
        var document = Parse("![alt]()");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        var image = AssertSingleInline<ImageInline>(paragraph.Inlines);
        Assert.Empty(image.Source);
        AssertSingleText(image.Alt, "alt");
    }

    [Fact]
    public void Parse_ImageWithEmptyAlt_IsAccepted()
    {
        // An empty alt is valid; a presentation layer may still render the image.
        var document = Parse("![](tennis.png)");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        var image = AssertSingleInline<ImageInline>(paragraph.Inlines);
        Assert.Equal("tennis.png", image.Source);
        Assert.Empty(image.Alt);
    }

    [Fact]
    public void Parse_ImageAltWithCodeSpan_PreservesStructure()
    {
        // An alt is inline content, so a code span inside it is kept as structure.
        var document = Parse("![a `b` c](x.png)");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        var image = AssertSingleInline<ImageInline>(paragraph.Inlines);
        Assert.Equal("x.png", image.Source);
        Assert.Collection(
            image.Alt,
            inline => AssertText(inline, "a "),
            inline => Assert.Equal("b", Assert.IsType<CodeSpanInline>(inline).Content),
            inline => AssertText(inline, " c"));
    }

    [Fact]
    public void Parse_ImageAmongText_KeepsSurroundingText()
    {
        // An image can sit inline with speech (e.g. a portrait or emoji mid-chat),
        // so the surrounding text stays its own runs.
        var document = Parse("see ![alt](x.png) end");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        Assert.Collection(
            paragraph.Inlines,
            inline => AssertText(inline, "see "),
            inline =>
            {
                var image = Assert.IsType<ImageInline>(inline);
                Assert.Equal("x.png", image.Source);
                AssertSingleText(image.Alt, "alt");
            },
            inline => AssertText(inline, " end"));
    }
}

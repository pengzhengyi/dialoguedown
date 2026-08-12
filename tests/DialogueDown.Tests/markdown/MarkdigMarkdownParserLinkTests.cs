using DialogueDown.Markdown;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.MarkdownAstAssert;

namespace DialogueDown.Tests.Markdown;

public sealed class MarkdigMarkdownParserLinkTests : MarkdigMarkdownParserTestBase
{
    [Fact]
    public void Parse_Link_ProducesLinkInlineWithTargetAndLabel()
    {
        var document = Parse("[Play tennis](#play-tennis)");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        var link = AssertSingleInline<LinkInline>(paragraph.Inlines);
        Assert.Equal("#play-tennis", link.Target);
        AssertSingleText(link.Label, "Play tennis");
    }

    [Fact]
    public void Parse_LinkLabelWithEmphasis_PreservesStructure()
    {
        // A label is inline content, so its emphasis is kept as structure, not flattened.
        var document = Parse("[Play *tennis*](#x)");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        var link = AssertSingleInline<LinkInline>(paragraph.Inlines);
        Assert.Equal("#x", link.Target);
        Assert.Collection(
            link.Label,
            inline => AssertText(inline, "Play "),
            inline =>
            {
                var emphasis = Assert.IsType<EmphasisInline>(inline);
                Assert.Equal(EmphasisKind.Italic, emphasis.Kind);
                AssertSingleText(emphasis.Children, "tennis");
            });
    }

    [Fact]
    public void Parse_LinkWithEmptyTarget_IsAccepted()
    {
        // Syntactically valid; an empty jump target is rejected later, not here.
        var document = Parse("[label]()");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        var link = AssertSingleInline<LinkInline>(paragraph.Inlines);
        Assert.Empty(link.Target);
        AssertSingleText(link.Label, "label");
    }

    [Fact]
    public void Parse_LinkWithEmptyLabel_IsAccepted()
    {
        // An empty label is syntactically valid; whether a jump needs display text
        // is decided downstream, not here.
        var document = Parse("[](#x)");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        var link = AssertSingleInline<LinkInline>(paragraph.Inlines);
        Assert.Equal("#x", link.Target);
        Assert.Empty(link.Label);
    }

    [Fact]
    public void Parse_LinkLabelWithCodeSpan_PreservesStructure()
    {
        // A code span inside a label is kept as a CodeSpanInline, not flattened to text.
        var document = Parse("[a `b` c](#x)");

        var paragraph = AssertSingleBlock<Paragraph>(document);
        var link = AssertSingleInline<LinkInline>(paragraph.Inlines);
        Assert.Equal("#x", link.Target);
        Assert.Collection(
            link.Label,
            inline => AssertText(inline, "a "),
            inline => Assert.Equal("b", Assert.IsType<CodeSpanInline>(inline).Content),
            inline => AssertText(inline, " c"));
    }
}

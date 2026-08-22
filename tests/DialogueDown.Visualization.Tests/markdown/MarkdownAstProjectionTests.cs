using DialogueDown.Common;
using DialogueDown.Markdown;
using DialogueDown.Visualization.Display;
using DialogueDown.Visualization.Markdown;

namespace DialogueDown.Visualization.Tests.Markdown;

public sealed class MarkdownAstProjectionTests
{
    private readonly MarkdownAstProjection _projection = new(new string('.', 64));

    [Fact]
    public void Constructor_NullSource_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new MarkdownAstProjection(null!));
    }

    [Fact]
    public void Title_IsMarkdownAst()
    {
        Assert.Equal("Markdown AST", _projection.Title);
    }

    [Fact]
    public void Description_IsANonEmptyOneLiner()
    {
        Assert.False(string.IsNullOrWhiteSpace(_projection.Description));
        Assert.DoesNotContain('\n', _projection.Description);
    }

    [Fact]
    public void Describe_NullNode_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _projection.Describe(null!));
    }

    [Fact]
    public void Describe_UnsupportedType_Throws()
    {
        Assert.Throws<ArgumentException>(() => _projection.Describe("not an AST node"));
    }

    [Fact]
    public void Describe_Document_LabelsDocumentWithNoAttributes()
    {
        var description = _projection.Describe(new MarkdownDocument([]));

        Assert.Equal("Document", description.Label);
        Assert.Empty(description.Attributes);
    }

    [Fact]
    public void Describe_Document_UsesWholeSourceAsSnippet()
    {
        var source =
            """
            # Hi

            World
            """;

        var description = new MarkdownAstProjection(source).Describe(new MarkdownDocument([]));

        Assert.Equal(source, description.Source);
    }

    [Fact]
    public void Describe_Node_CarriesSourceSnippetSlicedFromSpan()
    {
        var source = "# Hello";
        var heading = new Heading(1, [], new SourceSpan(0, 7));

        var description = new MarkdownAstProjection(source).Describe(heading);

        Assert.Equal("# Hello", description.Source);
    }

    [Fact]
    public void Describe_Node_WithSpanPastEnd_ClampsSnippet()
    {
        var source = "ab";
        var paragraph = new Paragraph([], new SourceSpan(1, 10));

        var description = new MarkdownAstProjection(source).Describe(paragraph);

        Assert.Equal("b", description.Source);
    }

    [Fact]
    public void Describe_BlockNode_CarriesTheStructuredSpan()
    {
        var description = new MarkdownAstProjection("# Hello").Describe(new Heading(1, [], new SourceSpan(0, 7)));

        Assert.Equal(new DisplaySpan(0, 7), description.Span);
    }

    [Fact]
    public void Describe_InlineNode_CarriesTheStructuredSpan()
    {
        var description = new MarkdownAstProjection("word").Describe(new TextInline("word", new SourceSpan(0, 4)));

        Assert.Equal(new DisplaySpan(0, 4), description.Span);
    }

    [Fact]
    public void Describe_Document_SpansTheWholeSource()
    {
        var source = "# Hi\n\nWorld";

        var description = new MarkdownAstProjection(source).Describe(new MarkdownDocument([]));

        Assert.Equal(new DisplaySpan(0, source.Length), description.Span);
    }

    [Fact]
    public void Describe_SpanPastEnd_ClampsTheSpanToo()
    {
        var description = new MarkdownAstProjection("ab").Describe(new Paragraph([], new SourceSpan(1, 10)));

        Assert.Equal(new DisplaySpan(1, 2), description.Span);
    }

    [Fact]
    public void Describe_AssignsSemanticCategoryPerNodeType()
    {
        var span = new SourceSpan(0, 2);

        // The categories are a cross-stage vocabulary: a code span is "call" so it
        // shares a color with the game call it later compiles to.
        Assert.Equal("document", _projection.Describe(new MarkdownDocument([])).Category);
        Assert.Equal("structure", _projection.Describe(new Heading(1, [], span)).Category);
        Assert.Equal("speech", _projection.Describe(new Paragraph([], span)).Category);
        Assert.Equal("choice", _projection.Describe(new ListBlock(false, [], span)).Category);
        Assert.Equal("choice", _projection.Describe(new ListItem([], span)).Category);
        Assert.Equal("structure", _projection.Describe(new QuoteBlock([], span)).Category);
        Assert.Equal("text", _projection.Describe(new TextInline("x", span)).Category);
        Assert.Equal("jump", _projection.Describe(new LinkInline("t", [], span)).Category);
        Assert.Equal("media", _projection.Describe(new ImageInline("s", [], span)).Category);
        Assert.Equal("call", _projection.Describe(new CodeSpanInline("c", span)).Category);
        Assert.Equal("styling", _projection.Describe(new EmphasisInline(EmphasisKind.Bold, [], span)).Category);
        Assert.Equal("break", _projection.Describe(new LineBreak(false, span)).Category);
    }

    [Fact]
    public void Describe_Heading_IncludesLevelAndSpan()
    {
        var description = _projection.Describe(new Heading(2, [], new SourceSpan(0, 8)));

        Assert.Equal("Heading (H2)", description.Label);
        Assert.Equal("2", Attribute(description, "level"));
        Assert.Equal("[0, 8)", Attribute(description, "span"));
    }

    [Fact]
    public void Describe_Paragraph_LabelsParagraphWithSpan()
    {
        var description = _projection.Describe(new Paragraph([], new SourceSpan(3, 5)));

        Assert.Equal("Paragraph", description.Label);
        Assert.Equal("[3, 8)", Attribute(description, "span"));
    }

    [Theory]
    [InlineData(true, "List (ordered)")]
    [InlineData(false, "List (unordered)")]
    public void Describe_ListBlock_ReflectsOrdering(bool ordered, string expectedLabel)
    {
        var description = _projection.Describe(new ListBlock(ordered, [], new SourceSpan(0, 4)));

        Assert.Equal(expectedLabel, description.Label);
    }

    [Fact]
    public void Describe_ListItem_LabelsListItem()
    {
        var description = _projection.Describe(new ListItem([], new SourceSpan(0, 2)));

        Assert.Equal("List item", description.Label);
    }

    [Fact]
    public void Describe_QuoteBlock_LabelsBlockQuote()
    {
        var description = _projection.Describe(new QuoteBlock([], new SourceSpan(0, 4)));

        Assert.Equal("Block quote", description.Label);
    }

    [Fact]
    public void Describe_Text_IncludesTextAndSpan()
    {
        var description = _projection.Describe(new TextInline("Hello", new SourceSpan(0, 5)));

        Assert.Equal("Text", description.Label);
        Assert.Equal("Hello", Attribute(description, "text"));
        Assert.Equal("[0, 5)", Attribute(description, "span"));
    }

    [Fact]
    public void Describe_Link_IncludesTargetAndLabel()
    {
        var description = _projection.Describe(
            new LinkInline("#scene", [new TextInline("Go", new SourceSpan(0, 2))], new SourceSpan(0, 12)));

        Assert.Equal("Link", description.Label);
        Assert.Equal("#scene", Attribute(description, "target"));
        Assert.Equal("Go", Attribute(description, "label"));
    }

    // A label is rarely plain text: it can hold a code span, styling, a nested link or image, and
    // a line break. Each contributes its own reading, so the flattened label stays legible rather
    // than dropping the styled or linked parts of what the writer wrote.
    [Fact]
    public void Describe_LinkLabel_FlattensEveryKindOfInlineItHolds()
    {
        var span = new SourceSpan(0, 4);
        var description = _projection.Describe(
            new LinkInline(
                "#scene",
                [
                    new TextInline("go ", span),
                    new CodeSpanInline("now", span),
                    new EmphasisInline(EmphasisKind.Bold, [new TextInline(" fast", span)], span),
                    new LineBreak(false, span),
                    new LinkInline("#inner", [new TextInline("there", span)], span),
                    new ImageInline("map.png", [new TextInline("map", span)], span),
                ],
                new SourceSpan(0, 48)));

        // Text, then the code span's content, the emphasized run, the break as a space, and the
        // nested link's and image's own text.
        Assert.Equal("go now fast theremap", Attribute(description, "label"));
    }

    [Fact]
    public void Describe_Image_IncludesSourceAndAlt()
    {
        var description = _projection.Describe(
            new ImageInline("hero.png", [new TextInline("Hero", new SourceSpan(0, 4))], new SourceSpan(0, 16)));

        Assert.Equal("Image", description.Label);
        Assert.Equal("hero.png", Attribute(description, "source"));
        Assert.Equal("Hero", Attribute(description, "alt"));
    }

    [Fact]
    public void Describe_CodeSpan_IncludesContent()
    {
        var description = _projection.Describe(new CodeSpanInline("hasKey", new SourceSpan(0, 8)));

        Assert.Equal("Code span", description.Label);
        Assert.Equal("hasKey", Attribute(description, "content"));
    }

    [Fact]
    public void Describe_Emphasis_IncludesKind()
    {
        var description = _projection.Describe(new EmphasisInline(EmphasisKind.Bold, [], new SourceSpan(0, 4)));

        Assert.Equal("Emphasis (Bold)", description.Label);
    }

    [Theory]
    [InlineData(true, "Line break (hard)")]
    [InlineData(false, "Line break (soft)")]
    public void Describe_LineBreak_ReflectsHardness(bool hard, string expectedLabel)
    {
        var description = _projection.Describe(new LineBreak(hard, new SourceSpan(0, 1)));

        Assert.Equal(expectedLabel, description.Label);
    }

    [Fact]
    public void Neighbors_NullNode_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _projection.Neighbors(null!));
    }

    [Fact]
    public void Neighbors_Document_ReturnsBlocks()
    {
        var paragraph = new Paragraph([], new SourceSpan(0, 1));
        var document = new MarkdownDocument([paragraph]);

        Assert.Equal(new object[] { paragraph }, _projection.Neighbors(document));
    }

    [Fact]
    public void Neighbors_Heading_ReturnsInlines()
    {
        var text = new TextInline("Hi", new SourceSpan(0, 2));
        var heading = new Heading(1, [text], new SourceSpan(0, 4));

        Assert.Equal(new object[] { text }, _projection.Neighbors(heading));
    }

    [Fact]
    public void Neighbors_Paragraph_ReturnsInlines()
    {
        var text = new TextInline("Hi", new SourceSpan(0, 2));
        var paragraph = new Paragraph([text], new SourceSpan(0, 2));

        Assert.Equal(new object[] { text }, _projection.Neighbors(paragraph));
    }

    [Fact]
    public void Neighbors_ListBlock_ReturnsItems()
    {
        var item = new ListItem([], new SourceSpan(0, 1));
        var list = new ListBlock(false, [item], new SourceSpan(0, 1));

        Assert.Equal(new object[] { item }, _projection.Neighbors(list));
    }

    [Fact]
    public void Neighbors_ListItem_ReturnsBlocks()
    {
        var paragraph = new Paragraph([], new SourceSpan(0, 1));
        var item = new ListItem([paragraph], new SourceSpan(0, 1));

        Assert.Equal(new object[] { paragraph }, _projection.Neighbors(item));
    }

    [Fact]
    public void Neighbors_QuoteBlock_ReturnsBlocks()
    {
        var paragraph = new Paragraph([], new SourceSpan(0, 1));
        var quote = new QuoteBlock([paragraph], new SourceSpan(0, 1));

        Assert.Equal(new object[] { paragraph }, _projection.Neighbors(quote));
    }

    [Fact]
    public void Neighbors_Emphasis_ReturnsChildren()
    {
        var text = new TextInline("Hi", new SourceSpan(0, 2));
        var emphasis = new EmphasisInline(EmphasisKind.Italic, [text], new SourceSpan(0, 4));

        Assert.Equal(new object[] { text }, _projection.Neighbors(emphasis));
    }

    [Fact]
    public void Neighbors_LeafInline_IsEmpty()
    {
        Assert.Empty(_projection.Neighbors(new TextInline("Hi", new SourceSpan(0, 2))));
    }

    private static string Attribute(NodeDescription description, string name) =>
        description.Attributes.Single(a => a.Name == name).Value;
}

using DialogueDown.Common;
using DialogueDown.Markdown;
using DialogueDown.Visualization.Display;
using DialogueDown.Visualization.Markdown;
using static DialogueDown.Visualization.Tests.Support.CompileAssert;

namespace DialogueDown.Visualization.Tests.Markdown;

public sealed class MarkdownAstProjectionTests
{
    // A script that holds every kind of Markdown node the projection describes.
    private const string EveryKind = """
        # The Market

        Alice: Fresh **apples**!
        Take one. ![An apple](apple.png)

        > `if` `Rich?`
        >
        > Alice: Buy two.

        - Alice: Thanks.
            => [The end](#END)
        """;

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
        var description = DescribeSingle<MarkdownDocument>("Hi");

        Assert.Equal("Document", description.Label);
        Assert.Empty(description.Attributes);
    }

    [Fact]
    public void Describe_Document_UsesWholeSourceAsSnippet()
    {
        const string Source = """
            # Hi

            World
            """;

        var description = DescribeSingle<MarkdownDocument>(Source);

        Assert.Equal(Source, description.Source);
    }

    [Fact]
    public void Describe_Node_CarriesSourceSnippetSlicedFromSpan()
    {
        const string Source = """
            # The Market

            Alice: Fresh apples!
            """;

        var description = DescribeSingle<Heading>(Source);

        Assert.Equal("# The Market", description.Source);
    }

    // The projection clamps a span that runs past the end of its source rather than throw. No
    // script in these tests produces one, so the node is built by hand.
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
        const string Source = """
            # The Market

            Alice: Fresh apples!
            """;

        var description = DescribeSingle<Heading>(Source);

        Assert.Equal(new DisplaySpan(0, 12), description.Span);
    }

    [Fact]
    public void Describe_InlineNode_CarriesTheStructuredSpan()
    {
        var description = DescribeSingle<TextInline>("Fresh apples!");

        Assert.Equal(new DisplaySpan(0, 13), description.Span);
    }

    [Fact]
    public void Describe_Document_SpansTheWholeSource()
    {
        const string Source = """
            # Hi

            World
            """;

        var description = DescribeSingle<MarkdownDocument>(Source);

        Assert.Equal(new DisplaySpan(0, Source.Length), description.Span);
    }

    // Like the snippet, a span past the end of the source is clamped; the node is built by hand
    // for the same reason.
    [Fact]
    public void Describe_SpanPastEnd_ClampsTheSpanToo()
    {
        var description = new MarkdownAstProjection("ab").Describe(new Paragraph([], new SourceSpan(1, 10)));

        Assert.Equal(new DisplaySpan(1, 2), description.Span);
    }

    [Fact]
    public void Describe_AssignsSemanticCategoryPerNodeType()
    {
        // The categories are a cross-stage vocabulary: a code span is "call" so it
        // shares a color with the game call it later compiles to.
        Assert.Equal("document", CategoryOf<MarkdownDocument>());
        Assert.Equal("structure", CategoryOf<Heading>());
        Assert.Equal("speech", CategoryOf<Paragraph>());
        Assert.Equal("choice", CategoryOf<ListBlock>());
        Assert.Equal("choice", CategoryOf<ListItem>());
        Assert.Equal("structure", CategoryOf<QuoteBlock>());
        Assert.Equal("text", CategoryOf<TextInline>());
        Assert.Equal("jump", CategoryOf<LinkInline>());
        Assert.Equal("media", CategoryOf<ImageInline>());
        Assert.Equal("call", CategoryOf<CodeSpanInline>());
        Assert.Equal("styling", CategoryOf<EmphasisInline>());
        Assert.Equal("break", CategoryOf<LineBreak>());
    }

    [Fact]
    public void Describe_Heading_IncludesLevelAndSpan()
    {
        const string Source = """
            ## The Market

            Alice: Fresh apples!
            """;

        var description = DescribeSingle<Heading>(Source);

        Assert.Equal("Heading (H2)", description.Label);
        Assert.Equal([new("level", "2"), new("span", "[0, 13)")], description.Attributes);
    }

    [Fact]
    public void Describe_Paragraph_LabelsParagraphWithSpan()
    {
        const string Source = """
            # The Market

            Alice: Fresh apples!
            """;

        var description = DescribeSingle<Paragraph>(Source);

        Assert.Equal("Paragraph", description.Label);
        Assert.Equal([new("span", "[14, 34)")], description.Attributes);
    }

    [Theory]
    [InlineData("1. Alice: Yes.", "List (ordered)")]
    [InlineData("- Alice: Yes.", "List (unordered)")]
    public void Describe_ListBlock_ReflectsOrdering(string source, string expectedLabel)
    {
        var description = DescribeSingle<ListBlock>(source);

        Assert.Equal(expectedLabel, description.Label);
    }

    [Fact]
    public void Describe_ListItem_LabelsListItem()
    {
        var description = DescribeSingle<ListItem>("- Alice: Yes.");

        Assert.Equal("List item", description.Label);
    }

    [Fact]
    public void Describe_QuoteBlock_LabelsBlockQuote()
    {
        const string Source = """
            > `if` `Rich?`
            >
            > Alice: Buy two.
            """;

        var description = DescribeSingle<QuoteBlock>(Source);

        Assert.Equal("Block quote", description.Label);
    }

    [Fact]
    public void Describe_Text_IncludesTextAndSpan()
    {
        var description = DescribeSingle<TextInline>("Hello");

        Assert.Equal("Text", description.Label);
        Assert.Equal([new("text", "Hello"), new("span", "[0, 5)")], description.Attributes);
    }

    [Fact]
    public void Describe_Link_IncludesTargetAndLabel()
    {
        var description = DescribeSingle<LinkInline>("=> [The end](#END)");

        Assert.Equal("Link", description.Label);
        Assert.Equal(
            [new("target", "#END"), new("label", "The end"), new("span", "[3, 18)")],
            description.Attributes);
    }

    // A label is rarely plain text: it can hold a code span, styling, a nested link or image, and
    // a line break. Each contributes its own reading, so the flattened label stays legible rather
    // than dropping the styled or linked parts of what the writer wrote. CommonMark forbids a link
    // inside a link, so no source produces this label; the tree is built by hand.
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

        // Text, then the code span's content, the emphasized text, the break as a space, and the
        // nested link's and image's own text.
        Assert.Equal("go now fast theremap", Attribute(description, "label"));
    }

    [Fact]
    public void Describe_Image_IncludesSourceAndAlt()
    {
        var description = DescribeSingle<ImageInline>("![Hero](hero.png)");

        Assert.Equal("Image", description.Label);
        Assert.Equal(
            [new("source", "hero.png"), new("alt", "Hero"), new("span", "[0, 17)")],
            description.Attributes);
    }

    [Fact]
    public void Describe_CodeSpan_IncludesContent()
    {
        var description = DescribeSingle<CodeSpanInline>("`HasKey?` Alice: Come in.");

        Assert.Equal("Code span", description.Label);
        Assert.Equal([new("content", "HasKey?"), new("span", "[0, 9)")], description.Attributes);
    }

    [Fact]
    public void Describe_Emphasis_IncludesKind()
    {
        var description = DescribeSingle<EmphasisInline>("Alice: **Hi**");

        Assert.Equal("Emphasis (Bold)", description.Label);
    }

    // A backslash at the end of a line is CommonMark's hard break; a bare newline is a soft one.
    [Theory]
    [InlineData(
        """
        Alice: Hi.\
        Bye.
        """,
        "Line break (hard)")]
    [InlineData(
        """
        Alice: Hi.
        Bye.
        """,
        "Line break (soft)")]
    public void Describe_LineBreak_ReflectsHardness(string source, string expectedLabel)
    {
        var description = DescribeSingle<LineBreak>(source);

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
        const string Source = """
            Alice: Hi.

            Bob: Hello.
            """;
        var document = AssertCompilesSingle<MarkdownDocument>(Source);

        AssertNeighbors(document, [.. document.Blocks]);
    }

    [Fact]
    public void Neighbors_Heading_ReturnsInlines()
    {
        var heading = AssertCompilesSingle<Heading>("# The Market");

        AssertNeighbors(heading, [.. heading.Inlines]);
    }

    [Fact]
    public void Neighbors_Paragraph_ReturnsInlines()
    {
        var paragraph = AssertCompilesSingle<Paragraph>("Alice: Fresh **apples**!");

        AssertNeighbors(paragraph, [.. paragraph.Inlines]);
    }

    [Fact]
    public void Neighbors_ListBlock_ReturnsItems()
    {
        const string Source = """
            - Alice: Yes.
            - Alice: No.
            """;
        var list = AssertCompilesSingle<ListBlock>(Source);

        AssertNeighbors(list, [.. list.Items]);
    }

    [Fact]
    public void Neighbors_ListItem_ReturnsBlocks()
    {
        var item = AssertCompilesSingle<ListItem>("- Alice: Yes.");

        AssertNeighbors(item, [.. item.Blocks]);
    }

    [Fact]
    public void Neighbors_QuoteBlock_ReturnsBlocks()
    {
        const string Source = """
            > `if` `Rich?`
            >
            > Alice: Buy two.
            """;
        var quote = AssertCompilesSingle<QuoteBlock>(Source);

        AssertNeighbors(quote, [.. quote.Blocks]);
    }

    [Fact]
    public void Neighbors_Emphasis_ReturnsChildren()
    {
        var emphasis = AssertCompilesSingle<EmphasisInline>("Alice: *Hi*");

        AssertNeighbors(emphasis, [.. emphasis.Children]);
    }

    [Fact]
    public void Neighbors_LeafInline_IsEmpty()
    {
        Assert.Empty(_projection.Neighbors(AssertCompilesSingle<TextInline>("Hello")));
    }

    // Describes the one node of the type given that the source compiles to, reading its snippet
    // and span from that source.
    private static NodeDescription DescribeSingle<TNode>(string source)
        where TNode : notnull =>
        new MarkdownAstProjection(source).Describe(AssertCompilesSingle<TNode>(source));

    // The category of the first node of the type given in a script that holds every kind.
    private static string? CategoryOf<TNode>()
        where TNode : notnull =>
        new MarkdownAstProjection(EveryKind).Describe(AssertCompiles<TNode>(EveryKind)[0]).Category;

    private static string Attribute(NodeDescription description, string name) =>
        description.Attributes.Single(a => a.Name == name).Value;

    // Asserts the projection yields exactly the children given, in order. The children are read
    // from the node itself, so a node with none would pass without checking anything; the guard
    // keeps every fixture meaningful.
    private void AssertNeighbors(object node, IReadOnlyList<object> expected)
    {
        Assert.NotEmpty(expected);
        Assert.Equal(expected, _projection.Neighbors(node));
    }
}

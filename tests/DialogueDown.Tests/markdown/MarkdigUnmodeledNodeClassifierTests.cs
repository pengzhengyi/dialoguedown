using DialogueDown.Markdown;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using static DialogueDown.Markdown.MarkdigUnmodeledNodeClassifier;

namespace DialogueDown.Tests.Markdown;

public sealed class MarkdigUnmodeledNodeClassifierTests
{
    [Fact]
    public void ClassifyBlock_MapsKnownBlocks()
    {
        Assert.Equal(UnmodeledNodeKind.CodeBlock, ClassifyBlock(FencedCode()));
        Assert.Equal(UnmodeledNodeKind.ThematicBreak, ClassifyBlock(ThematicBreak()));
        Assert.Equal(UnmodeledNodeKind.Table, ClassifyBlock(PipeTable()));
        Assert.Equal(UnmodeledNodeKind.BlockQuote, ClassifyBlock(Quote()));
        Assert.Equal(UnmodeledNodeKind.RawHtml, ClassifyBlock(HtmlBlockNode()));
    }

    [Fact]
    public void ClassifyBlock_UnrecognizedBlock_IsOther()
    {
        Assert.Equal(UnmodeledNodeKind.Other, ClassifyBlock(UnrecognizedBlock()));
    }

    [Fact]
    public void ClassifyInline_MapsKnownInlines()
    {
        Assert.Equal(UnmodeledNodeKind.Autolink, ClassifyInline(Autolink()));
        Assert.Equal(UnmodeledNodeKind.RawHtml, ClassifyInline(InlineHtml()));
    }

    [Fact]
    public void ClassifyInline_UnrecognizedInline_IsOther()
    {
        Assert.Equal(UnmodeledNodeKind.Other, ClassifyInline(UnrecognizedInline()));
    }

    // Markdig block constructors take a parser argument the classifier ignores;
    // these local factories hide that noise. Kept local — no other test builds
    // raw Markdig nodes (the rest go through the parser).
    private static Block FencedCode() => new FencedCodeBlock(null!);

    private static Block ThematicBreak() => new ThematicBreakBlock(null!);

    private static Block PipeTable() => new Table();

    private static Block Quote() => new Markdig.Syntax.QuoteBlock(null!);

    private static Block HtmlBlockNode() => new HtmlBlock(null!);

    private static Block UnrecognizedBlock() => new ParagraphBlock();

    private static Inline Autolink() => new AutolinkInline("https://x");

    private static Inline InlineHtml() => new HtmlInline("<b>");

    private static Inline UnrecognizedInline() => new LiteralInline("x");
}

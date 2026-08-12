using DialogueDown.Markdown;
using static DialogueDown.Markdown.MarkdigUnmodeledNodeClassifier;
using static DialogueDown.Tests.Support.MarkdigNodeFactory;

namespace DialogueDown.Tests.Markdown;

public sealed class MarkdigUnmodeledNodeClassifierTests
{
    [Fact]
    public void ClassifyBlock_MapsKnownBlocks()
    {
        Assert.Equal(UnmodeledNodeKind.CodeBlock, ClassifyBlock(FencedCode()));
        Assert.Equal(UnmodeledNodeKind.ThematicBreak, ClassifyBlock(ThematicBreak()));
        Assert.Equal(UnmodeledNodeKind.Table, ClassifyBlock(PipeTable()));
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
}

using DialogueDown.Markdown;
using DialogueDown.Visualization.Markdown;
using static DialogueDown.Visualization.Tests.Support.CompileAssert;

namespace DialogueDown.Visualization.Tests.Markdown;

public sealed class MarkdownDisplayExtensionsTests
{
    [Fact]
    public void ToDisplayGraph_TitlesMarkdownAstAndRootsAtDocument()
    {
        var document = AssertCompilesSingle<MarkdownDocument>("Hi");

        var graph = document.ToDisplayGraph("Hi");

        Assert.Equal("Markdown AST", graph.Title);
        Assert.Equal(["Document", "Paragraph", "Text"], graph.Nodes.Select(n => n.Label));
    }
}

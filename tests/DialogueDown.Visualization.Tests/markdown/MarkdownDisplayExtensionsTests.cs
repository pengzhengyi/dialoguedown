using DialogueDown.Common;
using DialogueDown.Markdown;
using DialogueDown.Visualization.Markdown;

namespace DialogueDown.Visualization.Tests.Markdown;

public sealed class MarkdownDisplayExtensionsTests
{
    [Fact]
    public void ToDisplayGraph_TitlesMarkdownAstAndRootsAtDocument()
    {
        var document = new MarkdownDocument(
        [
            new Paragraph([new TextInline("Hi", new SourceSpan(0, 2))], new SourceSpan(0, 2)),
        ]);

        var graph = document.ToDisplayGraph("Hi");

        Assert.Equal("Markdown AST", graph.Title);
        Assert.Equal("Document", graph.Nodes[0].Label);
        Assert.Contains(graph.Nodes, n => n.Label == "Paragraph");
        Assert.Contains(graph.Nodes, n => n.Label == "Text");
    }
}

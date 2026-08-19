using DialogueDown.Visualization.Render;
using static DialogueDown.Visualization.Tests.Support.Display;

namespace DialogueDown.Visualization.Tests.Render;

public sealed class DotRendererTests
{
    private readonly DotRenderer _renderer = new();

    [Fact]
    public void Render_NullGraph_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _renderer.Render(null!));
    }

    [Fact]
    public void Render_TreeGraph_EmitsDigraphWithTitleNodesAndEdge()
    {
        var graph = MakeGraph(
            "Markdown AST",
            [Node("n0", "Document"), Node("n1", "Paragraph")],
            [Child("n0", "n1")]);

        var output = _renderer.Render(graph);

        Assert.Equal(
            """
            digraph "Markdown AST" {
                n0 [label="Document"];
                n1 [label="Paragraph"];
                n0 -> n1;
            }

            """,
            output);
    }

    [Fact]
    public void Render_ReferenceEdge_IsDashed()
    {
        var graph = MakeGraph("G", [Node("n0", "a"), Node("n1", "b")], [Reference("n0", "n1")]);

        var output = _renderer.Render(graph);

        Assert.Contains("n0 -> n1 [style=dashed];", output);
    }

    [Fact]
    public void Render_IncludesAttributesOnSeparateLines()
    {
        var graph = MakeGraph(
            "G",
            [Node("n0", "Heading", Attr("level", "2"))],
            []);

        var output = _renderer.Render(graph);

        Assert.Contains("""n0 [label="Heading\nlevel: 2"];""", output);
    }

    [Fact]
    public void Render_EscapesQuotesAndBackslashesInLabel()
    {
        var graph = MakeGraph("G", [Node("n0", """a "b" \c""")], []);

        var output = _renderer.Render(graph);

        Assert.Contains("""n0 [label="a \"b\" \\c"];""", output);
    }
}

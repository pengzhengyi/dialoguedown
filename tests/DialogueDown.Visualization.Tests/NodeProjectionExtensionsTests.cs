using DialogueDown.Visualization.Display;
using DialogueDown.Visualization.Tests.Support;

namespace DialogueDown.Visualization.Tests;

public sealed class NodeProjectionExtensionsTests
{
    [Fact]
    public void ToDisplayGraph_WalksTheProjection()
    {
        var root = new Cell("root");
        var child = new Cell("child");
        var projection = new CellProjection { Title = "Cells" }.Link(root, child);

        var graph = root.ToDisplayGraph(projection);

        Assert.Equal("Cells", graph.Title);
        Assert.Equal(new[] { "root", "child" }, graph.Nodes.Select(n => n.Label));
        var edge = Assert.Single(graph.Edges);
        Assert.Equal(DisplayEdgeKind.Child, edge.Kind);
    }
}

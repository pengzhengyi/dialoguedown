using DialogueDown.Visualization.Display;
using DialogueDown.Visualization.Tests.Support;

namespace DialogueDown.Visualization.Tests;

public sealed class GraphWalkTests
{
    [Fact]
    public void Walk_NullRoot_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => GraphWalk.Walk<Cell>(null!, new CellProjection()));
    }

    [Fact]
    public void Walk_NullProjection_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => GraphWalk.Walk(new Cell("root"), null!));
    }

    [Fact]
    public void Walk_SingleNode_ProducesOneNodeAndNoEdges()
    {
        var root = new Cell("root");

        var graph = GraphWalk.Walk(root, new CellProjection());

        var node = Assert.Single(graph.Nodes);
        Assert.Equal("n0", node.Id);
        Assert.Equal("root", node.Label);
        Assert.Empty(graph.Edges);
    }

    [Fact]
    public void Walk_UsesProjectionTitle()
    {
        var graph = GraphWalk.Walk(new Cell("root"), new CellProjection { Title = "Markdown AST" });

        Assert.Equal("Markdown AST", graph.Title);
    }

    [Fact]
    public void Walk_UsesProjectionDescription()
    {
        var graph = GraphWalk.Walk(
            new Cell("root"),
            new CellProjection { Description = "What this stage shows." });

        Assert.Equal("What this stage shows.", graph.Description);
    }

    [Fact]
    public void Walk_IncludesLabelAndAttributes()
    {
        var root = new Cell("root");
        var projection = new CellProjection().WithAttributes(
            root,
            new DisplayAttribute("span", "[0, 3)"),
            new DisplayAttribute("kind", "leaf"));

        var node = Assert.Single(GraphWalk.Walk(root, projection).Nodes);

        Assert.Equal("root", node.Label);
        Assert.Collection(
            node.Attributes,
            a => Assert.Equal(new DisplayAttribute("span", "[0, 3)"), a),
            a => Assert.Equal(new DisplayAttribute("kind", "leaf"), a));
    }

    [Fact]
    public void Walk_CarriesSourceSnippet()
    {
        var root = new Cell("root");
        var projection = new CellProjection().WithSource(root, "# heading");

        var node = Assert.Single(GraphWalk.Walk(root, projection).Nodes);

        Assert.Equal("# heading", node.Source);
    }

    [Fact]
    public void Walk_CarriesCategory()
    {
        var root = new Cell("root");
        var projection = new CellProjection().WithCategory(root, "call");

        var node = Assert.Single(GraphWalk.Walk(root, projection).Nodes);

        Assert.Equal("call", node.Category);
    }

    [Fact]
    public void Walk_CarriesTypeName()
    {
        var root = new Cell("root");
        var projection = new CellProjection().WithTypeName(root, "Scene");

        var node = Assert.Single(GraphWalk.Walk(root, projection).Nodes);

        Assert.Equal("Scene", node.TypeName);
    }

    [Fact]
    public void Walk_CarriesRefKey()
    {
        var root = new Cell("root");
        var projection = new CellProjection().WithRefKey(root, "scene:the-market");

        var node = Assert.Single(GraphWalk.Walk(root, projection).Nodes);

        Assert.Equal("scene:the-market", node.RefKey);
    }

    [Fact]
    public void Walk_Tree_EmitsChildEdgesInPreOrderWithSequentialIds()
    {
        // root ─┬─ a ── c
        //       └─ b
        var root = new Cell("root");
        var a = new Cell("a");
        var b = new Cell("b");
        var c = new Cell("c");
        var projection = new CellProjection().Link(root, a, b).Link(a, c);

        var graph = GraphWalk.Walk(root, projection);

        Assert.Equal(new[] { "root", "a", "c", "b" }, graph.Nodes.Select(n => n.Label));
        Assert.Equal(new[] { "n0", "n1", "n2", "n3" }, graph.Nodes.Select(n => n.Id));
        Assert.Equal(3, graph.Edges.Count);
        Assert.All(graph.Edges, e => Assert.Equal(DisplayEdgeKind.Child, e.Kind));
    }

    [Fact]
    public void Walk_ValueEqualSiblings_AreDistinctNodes()
    {
        // Two cells named "x" are value-equal; a reference-identity walk keeps them
        // separate (three nodes, two child edges) instead of merging them.
        var root = new Cell("root");
        var x1 = new Cell("x");
        var x2 = new Cell("x");
        Assert.Equal(x1, x2);
        var projection = new CellProjection().Link(root, x1, x2);

        var graph = GraphWalk.Walk(root, projection);

        Assert.Equal(3, graph.Nodes.Count);
        Assert.Equal(2, graph.Edges.Count);
        Assert.All(graph.Edges, e => Assert.Equal(DisplayEdgeKind.Child, e.Kind));
    }

    [Fact]
    public void Walk_SharedNode_EmitsReferenceEdgeOnSecondParent()
    {
        // root ─┬─ a ─┐
        //       └─ b ─┴─ shared   (a first reaches shared; b references it)
        var root = new Cell("root");
        var a = new Cell("a");
        var b = new Cell("b");
        var shared = new Cell("shared");
        var projection = new CellProjection()
            .Link(root, a, b)
            .Link(a, shared)
            .Link(b, shared);

        var graph = GraphWalk.Walk(root, projection);

        Assert.Equal(4, graph.Nodes.Count); // shared appears exactly once
        var bId = graph.Nodes.Single(n => n.Label == "b").Id;
        var sharedId = graph.Nodes.Single(n => n.Label == "shared").Id;
        var reference = Assert.Single(graph.Edges, e => e.Kind == DisplayEdgeKind.Reference);
        Assert.Equal(bId, reference.FromId);
        Assert.Equal(sharedId, reference.ToId);
    }

    [Fact]
    public void Walk_Cycle_TerminatesAndEmitsReferenceEdge()
    {
        // a -> b -> a: the back-edge b -> a is a reference edge, so the walk stops.
        var a = new Cell("a");
        var b = new Cell("b");
        var projection = new CellProjection().Link(a, b).Link(b, a);

        var graph = GraphWalk.Walk(a, projection);

        Assert.Equal(2, graph.Nodes.Count);
        Assert.Equal(2, graph.Edges.Count);
        var aId = graph.Nodes.Single(n => n.Label == "a").Id;
        var bId = graph.Nodes.Single(n => n.Label == "b").Id;
        Assert.Contains(graph.Edges, e => e.FromId == aId && e.ToId == bId && e.Kind == DisplayEdgeKind.Child);
        Assert.Contains(graph.Edges, e => e.FromId == bId && e.ToId == aId && e.Kind == DisplayEdgeKind.Reference);
    }

    [Fact]
    public void Walk_SelfLoop_EmitsReferenceEdgeToSelf()
    {
        var a = new Cell("a");
        var projection = new CellProjection().Link(a, a);

        var graph = GraphWalk.Walk(a, projection);

        var node = Assert.Single(graph.Nodes);
        var edge = Assert.Single(graph.Edges);
        Assert.Equal(DisplayEdgeKind.Reference, edge.Kind);
        Assert.Equal(node.Id, edge.FromId);
        Assert.Equal(node.Id, edge.ToId);
    }

    [Fact]
    public void Walk_AStageWalkedFromATree_NestsItsChildren()
    {
        // A walk's child edge is the tree it descended, so a parent's reach includes its children.
        var root = new Cell("root");
        var projection = new CellProjection().Link(root, new Cell("leaf"));

        var graph = GraphWalk.Walk(root, projection);

        Assert.True(graph.Nests);
    }

    [Fact]
    public void Walk_NullNeighbor_Throws()
    {
        var root = new Cell("root");
        var projection = new CellProjection().Link(root, (Cell)null!);

        Assert.Throws<ArgumentNullException>(() => GraphWalk.Walk(root, projection));
    }
}

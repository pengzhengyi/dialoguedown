using DialogueDown.Graph;
using DialogueDown.Graph.Edges;
using DialogueDown.Graph.Nodes;
using DialogueDown.Script.Ast;

namespace DialogueDown.Tests.Support;

/// <summary>Assertions over a built <see cref="DialogueGraph"/>, its nodes, and their edges.</summary>
internal static class GraphAssert
{
    /// <summary>Asserts the edge is a succession to <paramref name="target"/>.</summary>
    public static SuccessionEdge AssertSuccession(Edge edge, NodeId target)
    {
        var succession = Assert.IsType<SuccessionEdge>(edge);
        Assert.Equal(target, succession.Target);
        return succession;
    }

    /// <summary>Asserts the edge is a divert to <paramref name="target"/>.</summary>
    public static DivertEdge AssertDivert(Edge edge, NodeId target)
    {
        var divert = Assert.IsType<DivertEdge>(edge);
        Assert.Equal(target, divert.Target);
        return divert;
    }

    /// <summary>Asserts the node's only out-edge is a succession to <paramref name="target"/>.</summary>
    /// <summary>
    /// Asserts the node's one fall-through leads to <paramref name="target"/>, beside whatever
    /// other routes it offers — unlike <see cref="AssertOnlySuccession"/>, which asserts the
    /// fall-through is the only edge the node has.
    /// </summary>
    public static void AssertFallsThroughTo(DialogueNode node, NodeId target)
    {
        ArgumentNullException.ThrowIfNull(node);
        AssertSuccession(Assert.Single(node.Out.OfType<SuccessionEdge>()), target);
    }

    /// <summary>Asserts the node has no fall-through, so control always leaves it another way.</summary>
    public static void AssertNoFallThrough(DialogueNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Assert.Empty(node.Out.OfType<SuccessionEdge>());
    }

    public static void AssertOnlySuccession(DialogueNode node, NodeId target)
    {
        ArgumentNullException.ThrowIfNull(node);
        AssertSuccession(Assert.Single(node.Out), target);
    }

    /// <summary>Asserts the node's only out-edge is a divert to <paramref name="target"/>.</summary>
    public static DivertEdge AssertOnlyDivert(DialogueNode node, NodeId target)
    {
        ArgumentNullException.ThrowIfNull(node);
        return AssertDivert(Assert.Single(node.Out), target);
    }

    /// <summary>Asserts the edge is guarded by <paramref name="key"/>.</summary>
    public static void AssertGuarded(Edge edge, string key) =>
        Assert.Equal(key, Assert.IsAssignableFrom<IGuardedEdge>(edge).Guard?.Key);

    /// <summary>
    /// Asserts the edge is the branch arm tried at <paramref name="order"/>, guarded by
    /// <paramref name="guard"/> — null for the <c>else</c> arm, which is always taken when reached.
    /// </summary>
    public static void AssertBranch(Edge edge, int order, string? guard)
    {
        var branch = Assert.IsType<BranchEdge>(edge);
        Assert.Equal(order, branch.Order);
        Assert.Equal(guard, branch.Guard?.Key);
    }

    /// <summary>Asserts the node's content plays only under <paramref name="key"/>.</summary>
    public static void AssertGuarded(DialogueNode node, string key) =>
        Assert.Equal(key, Assert.IsAssignableFrom<IGuardedNode>(node).Guard?.Key);

    /// <summary>Asserts the node's content always plays.</summary>
    public static void AssertUnguarded(DialogueNode node) =>
        Assert.Null(Assert.IsAssignableFrom<IGuardedNode>(node).Guard);

    /// <summary>Asserts the edge is one control may always take.</summary>
    public static void AssertUnguarded(Edge edge) =>
        Assert.Null(Assert.IsAssignableFrom<IGuardedEdge>(edge).Guard);

    /// <summary>Asserts the edge is a random arm weighted at <paramref name="percentage"/>.</summary>
    public static void AssertNumberWeight(Edge edge, double percentage) =>
        Assert.Equal(
            percentage,
            Assert.IsType<NumberWeight>(Assert.IsType<RandomOptionEdge>(edge).Weight).Percentage);

    /// <summary>Asserts the node's out-edges point at exactly <paramref name="targets"/>, in order.</summary>
    public static void AssertTargets(DialogueNode node, params NodeId[] targets)
    {
        ArgumentNullException.ThrowIfNull(node);
        Assert.Equal(targets, node.Out.Select(edge => edge.Target));
    }
}

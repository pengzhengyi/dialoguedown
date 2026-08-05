using DialogueDown.Graph;
using DialogueDown.Graph.Nodes;

namespace DialogueDown.Tests.Support;

/// <summary>Assertions over a built <see cref="DialogueGraph"/>, its nodes, and their edges.</summary>
internal static class GraphAssert
{
    /// <summary>Asserts the edge is a succession to <paramref name="target"/>.</summary>
    public static Succession AssertSuccession(Edge edge, NodeId target)
    {
        var succession = Assert.IsType<Succession>(edge);
        Assert.Equal(target, succession.Target);
        return succession;
    }

    /// <summary>Asserts the edge is a divert to <paramref name="target"/>.</summary>
    public static Divert AssertDivert(Edge edge, NodeId target)
    {
        var divert = Assert.IsType<Divert>(edge);
        Assert.Equal(target, divert.Target);
        return divert;
    }

    /// <summary>Asserts the node's only out-edge is a succession to <paramref name="target"/>.</summary>
    public static void AssertOnlySuccession(DialogueNode node, NodeId target)
    {
        ArgumentNullException.ThrowIfNull(node);
        AssertSuccession(Assert.Single(node.Out), target);
    }

    /// <summary>Asserts the node's only out-edge is a divert to <paramref name="target"/>.</summary>
    public static Divert AssertOnlyDivert(DialogueNode node, NodeId target)
    {
        ArgumentNullException.ThrowIfNull(node);
        return AssertDivert(Assert.Single(node.Out), target);
    }

    /// <summary>Asserts the node's out-edges point at exactly <paramref name="targets"/>, in order.</summary>
    public static void AssertTargets(DialogueNode node, params NodeId[] targets)
    {
        ArgumentNullException.ThrowIfNull(node);
        Assert.Equal(targets, node.Out.Select(edge => edge.Target));
    }
}

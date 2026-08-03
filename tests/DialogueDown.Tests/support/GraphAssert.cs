using DialogueDown.Graph;

namespace DialogueDown.Tests.Support;

/// <summary>Assertions over a built <see cref="DialogueGraph"/> and its nodes.</summary>
internal static class GraphAssert
{
    /// <summary>Asserts the node's only out-edge is a succession to <paramref name="target"/>.</summary>
    public static void AssertSuccession(DialogueNode node, NodeId target)
    {
        ArgumentNullException.ThrowIfNull(node);
        var succession = Assert.IsType<Succession>(Assert.Single(node.Out));
        Assert.Equal(target, succession.Target);
    }
}

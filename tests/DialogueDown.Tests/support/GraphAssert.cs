using DialogueDown.Graph;

namespace DialogueDown.Tests.Support;

/// <summary>Assertions over a built <see cref="DialogueGraph"/> and its nodes.</summary>
internal static class GraphAssert
{
    /// <summary>Asserts the node's only out-edge is a succession to <paramref name="target"/>.</summary>
    public static void AssertOnlySuccession(DialogueNode node, NodeId target)
    {
        ArgumentNullException.ThrowIfNull(node);
        var succession = Assert.IsType<Succession>(Assert.Single(node.Out));
        Assert.Equal(target, succession.Target);
    }

    /// <summary>Asserts the node's only out-edge is a divert to <paramref name="target"/>, and returns it.</summary>
    public static Divert AssertOnlyDivert(DialogueNode node, NodeId target)
    {
        ArgumentNullException.ThrowIfNull(node);
        var divert = Assert.IsType<Divert>(Assert.Single(node.Out));
        Assert.Equal(target, divert.Target);
        return divert;
    }
}

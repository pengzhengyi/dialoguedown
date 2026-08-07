using DialogueDown.Graph;

namespace DialogueDown.Tests.Support;

/// <summary>Assertions for opaque graph node ids.</summary>
internal static class NodeIdAssert
{
    public static void AssertIdEqual(int expected, NodeId actual) =>
        Assert.Equal(new NodeId(expected), actual);
}

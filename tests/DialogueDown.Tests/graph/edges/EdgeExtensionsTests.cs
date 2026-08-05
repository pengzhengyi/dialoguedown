using DialogueDown.Graph;
using DialogueDown.Graph.Edges;
using DialogueDown.Script.Ast;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueGraphFactory;

namespace DialogueDown.Tests.Graph.Edges;

public sealed class EdgeExtensionsTests
{
    private static readonly NodeId _target = NodeId(1);

    [Fact]
    public void LeavesUnconditionally_UnguardedDivert_IsTrue()
    {
        IReadOnlyList<Edge> edges = [new DivertEdge(_target)];

        Assert.True(edges.LeavesUnconditionally());
    }

    [Fact]
    public void LeavesUnconditionally_GuardedDivert_IsFalse()
    {
        IReadOnlyList<Edge> edges = [new DivertEdge(_target, new Condition("Brave", SourceSpanFactory.Span()))];

        Assert.False(edges.LeavesUnconditionally());
    }

    [Fact]
    public void LeavesUnconditionally_Option_IsTrue()
    {
        // A choice always takes one of its options, so it never falls through.
        IReadOnlyList<Edge> edges = [new OptionEdge(_target)];

        Assert.True(edges.LeavesUnconditionally());
    }

    [Fact]
    public void LeavesUnconditionally_OnlySuccession_IsFalse()
    {
        IReadOnlyList<Edge> edges = [new SuccessionEdge(_target)];

        Assert.False(edges.LeavesUnconditionally());
    }
}

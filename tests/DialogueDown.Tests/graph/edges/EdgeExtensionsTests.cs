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
        IReadOnlyList<Edge> edges = [new DivertEdge(_target, Guard("Brave"))];

        Assert.False(edges.LeavesUnconditionally());
    }

    [Fact]
    public void LeavesUnconditionally_UnguardedOption_IsTrue()
    {
        // The arm is always offered, so the choice always leaves through one of them.
        IReadOnlyList<Edge> edges = [new OptionEdge(_target)];

        Assert.True(edges.LeavesUnconditionally());
    }

    [Fact]
    public void LeavesUnconditionally_EveryOptionGuarded_IsFalse()
    {
        // Each guard may read false, so the choice can end up offering nothing.
        IReadOnlyList<Edge> edges =
        [
            new OptionEdge(_target, Guard("HasKey")),
            new OptionEdge(NodeId(2), Guard("HasRope")),
        ];

        Assert.False(edges.LeavesUnconditionally());
    }

    [Fact]
    public void LeavesUnconditionally_OneUnguardedOptionAmongGuardedOnes_IsTrue()
    {
        IReadOnlyList<Edge> edges =
        [
            new OptionEdge(_target, Guard("HasKey")),
            new OptionEdge(NodeId(2)),
        ];

        Assert.True(edges.LeavesUnconditionally());
    }

    [Fact]
    public void LeavesUnconditionally_OnlySuccession_IsFalse()
    {
        IReadOnlyList<Edge> edges = [new SuccessionEdge(_target)];

        Assert.False(edges.LeavesUnconditionally());
    }

    private static Condition Guard(string key) => new(key, SourceSpanFactory.Span());
}

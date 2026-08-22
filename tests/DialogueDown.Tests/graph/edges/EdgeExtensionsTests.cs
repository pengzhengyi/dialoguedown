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
    public void HasUnconditionalRoute_UnconditionalDivert_IsTrue()
    {
        IReadOnlyList<Edge> edges = [DivertEdge(_target)];

        Assert.True(edges.HasUnconditionalRoute());
    }

    [Fact]
    public void HasUnconditionalRoute_ConditionalDivert_IsFalse()
    {
        IReadOnlyList<Edge> edges = [DivertEdge(_target, Condition("Brave"))];

        Assert.False(edges.HasUnconditionalRoute());
    }

    [Fact]
    public void HasUnconditionalRoute_UnconditionalOption_IsTrue()
    {
        // The arm is always offered, so the choice always leaves through one of them.
        IReadOnlyList<Edge> edges = [new OptionEdge(_target)];

        Assert.True(edges.HasUnconditionalRoute());
    }

    [Fact]
    public void HasUnconditionalRoute_EveryOptionConditional_IsFalse()
    {
        // Each condition may read false, so the choice can end up offering nothing.
        IReadOnlyList<Edge> edges =
        [
            new OptionEdge(_target, Condition("HasKey")),
            new OptionEdge(NodeId(2), Condition("HasRope")),
        ];

        Assert.False(edges.HasUnconditionalRoute());
    }

    [Fact]
    public void HasUnconditionalRoute_OneUnconditionalOptionAmongConditionalOnes_IsTrue()
    {
        IReadOnlyList<Edge> edges =
        [
            new OptionEdge(_target, Condition("HasKey")),
            new OptionEdge(NodeId(2)),
        ];

        Assert.True(edges.HasUnconditionalRoute());
    }

    [Fact]
    public void HasUnconditionalRoute_OnlySuccession_IsFalse()
    {
        IReadOnlyList<Edge> edges = [new SuccessionEdge(_target)];

        Assert.False(edges.HasUnconditionalRoute());
    }

    private static Condition Condition(string key) => new(key, SourceSpanFactory.Span());
}

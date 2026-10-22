using DialogueDown.Graph.Builder;
using DialogueDown.Graph.Edges;
using DialogueDown.Graph.Nodes;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstFactory;
using static DialogueDown.Tests.Support.DialogueGraphFactory;
using static DialogueDown.Tests.Support.GraphAssert;

namespace DialogueDown.Tests.Graph.Builder;

public sealed class RandomChoiceNodeDraftTests
{
    [Fact]
    public void Freeze_CarriesTheWeightedEdgesAddedToIt()
    {
        var draft = new RandomChoiceNodeDraft(NodeId(0), SourceSpanFactory.Span());
        draft.AddEdge(new RandomOptionEdge(NodeId(1), NumberWeight(80)));
        draft.AddEdge(new RandomOptionEdge(NodeId(2), NumberWeight(20)));

        var node = Assert.IsType<RandomChoiceNode>(draft.Freeze());

        AssertTargets(node, NodeId(1), NodeId(2));
    }

    [Fact]
    public void LeavesUnconditionally_WithAnUnguardedNumberAboveZero_IsTrue()
    {
        var draft = ARandomChoiceOf(
            RandomOptionEdge(NodeId(1), NumberWeight(30)),
            RandomOptionEdge(NodeId(2), QueryWeight("Guard.Mood")));

        Assert.True(draft.LeavesUnconditionally());
    }

    [Fact]
    public void LeavesUnconditionally_WithAnUnguardedAutoWeight_IsTrue()
    {
        // Beside an auto weight in play, the shares in play add up to at least 100.
        var draft = ARandomChoiceOf(
            RandomOptionEdge(NodeId(1), QueryWeight("Guard.Mood")),
            RandomOptionEdge(NodeId(2), AutoWeight()));

        Assert.True(draft.LeavesUnconditionally());
    }

    [Fact]
    public void LeavesUnconditionally_WithOnlyKeyWeights_IsFalse()
    {
        // The game can answer 0 for each, which leaves nothing to draw.
        var draft = ARandomChoiceOf(
            RandomOptionEdge(NodeId(1), QueryWeight("Bob.Affection")),
            RandomOptionEdge(NodeId(2), QueryWeight("Christina.Affection")));

        Assert.False(draft.LeavesUnconditionally());
    }

    [Fact]
    public void LeavesUnconditionally_WithAnUnguardedZeroBesideAGuardedNumber_IsFalse()
    {
        // When the guard reads false, only the 0% is left in play.
        var draft = ARandomChoiceOf(
            RandomOptionEdge(NodeId(1), NumberWeight(50), Condition("Guard.IsAngry")),
            RandomOptionEdge(NodeId(2), NumberWeight(0)));

        Assert.False(draft.LeavesUnconditionally());
    }

    [Fact]
    public void LeavesUnconditionally_WithEveryRandomOptionGuarded_IsFalse()
    {
        var draft = ARandomChoiceOf(
            RandomOptionEdge(NodeId(1), AutoWeight(), Condition("Guard.IsAngry")),
            RandomOptionEdge(NodeId(2), NumberWeight(50), Condition("Guard.IsTired")));

        Assert.False(draft.LeavesUnconditionally());
    }

    private static RandomChoiceNodeDraft ARandomChoiceOf(params RandomOptionEdge[] randomOptions)
    {
        var draft = new RandomChoiceNodeDraft(NodeId(0), SourceSpanFactory.Span());
        foreach (var randomOption in randomOptions)
        {
            draft.AddEdge(randomOption);
        }

        return draft;
    }
}

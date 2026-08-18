using DialogueDown.Common;
using DialogueDown.Graph;
using DialogueDown.Graph.Builder;
using DialogueDown.Graph.Edges;
using DialogueDown.Graph.Nodes;
using DialogueDown.Script.Semantics;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueGraphFactory;

namespace DialogueDown.Tests.Graph.Builder;

public sealed class NodeDraftTests
{
    [Fact]
    public void Freeze_CombinesTheNodeIdAndAccumulatedEdges_ThenPreventsMutation()
    {
        var draft = new TestNodeDraft(NodeId(0));
        draft.AddSuccessionEdge(1);

        var node = Assert.IsType<TestNode>(draft.Freeze());

        Assert.Equal(draft.Id, node.Id);
        Assert.IsType<SuccessionEdge>(Assert.Single(node.Out));
        Assert.Throws<InvalidOperationException>(
            () => draft.AddSuccessionEdge(2));
    }

    [Fact]
    public void LeavesUnconditionally_UnconditionalRoute_IsTrue()
    {
        var draft = new TestNodeDraft(NodeId(0));
        draft.AddEdge(new DivertEdge(NodeId(1)));

        Assert.True(draft.LeavesUnconditionally());
    }

    [Fact]
    public void LeavesUnconditionally_NoRouteAtAll_IsFalse() =>
        Assert.False(new TestNodeDraft(NodeId(0)).LeavesUnconditionally());

    [Fact]
    public void LeavesUnconditionally_ConditionalNodeWithAnUnconditionalRoute_IsFalse()
    {
        // The condition may skip the node whole, so its divert is not a route control always takes.
        var draft = new LineNodeDraft(
            NodeId(0),
            SourceSpanFactory.Span(),
            SpeakerSymbol.ForName("Alice"),
            [],
            DialogueAstFactory.Condition("Brave"));
        draft.AddEdge(new DivertEdge(NodeId(1)));

        Assert.False(draft.LeavesUnconditionally());
    }

    private sealed class TestNodeDraft(NodeId id)
        : NodeDraft(id, SourceSpanFactory.Span())
    {
        protected override DialogueNode CreateNode() =>
            new TestNode(Id, Span, Out.ToArray());
    }

    private sealed record TestNode(NodeId Id, SourceSpan Span, IReadOnlyList<Edge> Out) :
        DialogueNode(Id, Span, Out);
}

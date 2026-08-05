using DialogueDown.Graph;
using DialogueDown.Graph.Builder;
using DialogueDown.Graph.Edges;
using DialogueDown.Graph.Nodes;
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

    private sealed class TestNodeDraft(NodeId id) : NodeDraft(id)
    {
        protected override DialogueNode CreateNode() =>
            new TestNode(Id, Out.ToArray());
    }

    private sealed record TestNode(NodeId Id, IReadOnlyList<Edge> Out) :
        DialogueNode(Id, Out);
}

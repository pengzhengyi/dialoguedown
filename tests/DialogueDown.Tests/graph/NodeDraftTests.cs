using DialogueDown.Graph;

namespace DialogueDown.Tests.Graph;

public sealed class NodeDraftTests
{
    [Fact]
    public void Freeze_CombinesTheNodeIdAndAccumulatedEdges_ThenPreventsMutation()
    {
        var draft = new TestNodeDraft(new NodeId(0));
        draft.AddEdge(new Succession(new NodeId(1)));

        var node = Assert.IsType<TestNode>(draft.Freeze());

        Assert.Equal(draft.Id, node.Id);
        Assert.IsType<Succession>(Assert.Single(node.Out));
        Assert.Throws<InvalidOperationException>(
            () => draft.AddEdge(new Succession(new NodeId(2))));
    }

    private sealed class TestNodeDraft(NodeId id) : NodeDraft(id)
    {
        protected override DialogueNode CreateNode(IReadOnlyList<Edge> edges) =>
            new TestNode(Id, edges);
    }

    private sealed record TestNode(NodeId Id, IReadOnlyList<Edge> Out) :
        DialogueNode(Id, Out);
}

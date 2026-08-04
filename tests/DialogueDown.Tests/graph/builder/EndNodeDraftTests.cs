using DialogueDown.Graph;
using DialogueDown.Graph.Builder;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueGraphFactory;

namespace DialogueDown.Tests.Graph.Builder;

public sealed class EndNodeDraftTests
{
    [Fact]
    public void Freeze_NoEdges_CreatesTheEndNode()
    {
        var draft = new EndNodeDraft(NodeId(0));

        Assert.IsType<EndNode>(draft.Freeze());
    }

    [Fact]
    public void Freeze_OutgoingEdge_Throws()
    {
        var draft = new EndNodeDraft(NodeId(0));
        draft.AddSuccessionEdge(1);

        Assert.Throws<InvalidOperationException>(() => draft.Freeze());
    }
}

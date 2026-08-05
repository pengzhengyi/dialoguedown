using DialogueDown.Graph;
using DialogueDown.Graph.Builder;
using DialogueDown.Graph.Edges;
using DialogueDown.Graph.Nodes;
using static DialogueDown.Tests.Support.DialogueGraphFactory;
using static DialogueDown.Tests.Support.GraphAssert;

namespace DialogueDown.Tests.Graph.Builder;

public sealed class ChoiceNodeDraftTests
{
    [Fact]
    public void Freeze_CarriesTheOptionEdgesAddedToIt()
    {
        var draft = new ChoiceNodeDraft(NodeId(0));
        draft.AddEdge(new OptionEdge(NodeId(1)));
        draft.AddEdge(new OptionEdge(NodeId(2)));

        var node = Assert.IsType<ChoiceNode>(draft.Freeze());

        AssertTargets(node, NodeId(1), NodeId(2));
    }
}

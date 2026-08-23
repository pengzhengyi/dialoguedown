using DialogueDown.Graph.Builder;
using DialogueDown.Graph.Nodes;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueGraphFactory;
using static DialogueDown.Tests.Support.GraphAssert;

namespace DialogueDown.Tests.Graph.Builder;

public sealed class ChoiceNodeDraftTests
{
    [Fact]
    public void Freeze_CarriesTheOptionEdgesAddedToIt()
    {
        var draft = new ChoiceNodeDraft(NodeId(0), SourceSpanFactory.Span(), isOrdered: true);
        draft.AddEdge(OptionEdge(NodeId(1)));
        draft.AddEdge(OptionEdge(NodeId(2)));

        var node = Assert.IsType<ChoiceNode>(draft.Freeze());

        Assert.True(node.IsOrdered);
        AssertTargets(node, NodeId(1), NodeId(2));
    }
}

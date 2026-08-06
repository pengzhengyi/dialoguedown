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
}

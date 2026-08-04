using DialogueDown.Graph;
using DialogueDown.Graph.Builder;
using DialogueDown.Script.Ast;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueGraphFactory;
using static DialogueDown.Tests.Support.GraphAssert;

namespace DialogueDown.Tests.Graph.Builder;

public sealed class ControlNodeDraftTests
{
    [Fact]
    public void Freeze_CombinesItsEffectsWithAccumulatedEdges()
    {
        var effects = new GameCall[] { new DefaultCommand("open the gate", SourceSpanFactory.Span()) };
        var draft = new ControlNodeDraft(NodeId(0), effects);
        draft.AddSuccessionEdge(1);

        var node = Assert.IsType<ControlNode>(draft.Freeze());

        Assert.Same(effects, node.Effects);
        AssertOnlySuccession(node, NodeId(1));
    }
}

using DialogueDown.Emission;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Script.Semantics;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstFactory;
using static DialogueDown.Tests.Support.DialogueGraphFactory;
using static DialogueDown.Tests.Support.PlaybookEdgeAssert;
using static DialogueDown.Tests.Support.SpeechAssert;
using GraphNodes = DialogueDown.Graph.Nodes;
using PlaybookEdges = DialogueDown.Playbook.Edges;

namespace DialogueDown.Tests.Emission;

public sealed class NodeMappingTests
{
    private static readonly SpeakerSymbol _alice = SpeakerSymbol.ForName("Alice");

    // Ids that are not positions, so anything copied straight off the graph would be wrong.
    private static readonly IReadOnlyList<GraphNodes.DialogueNode> _graph =
        [LineNode(12, _alice, Text("Hello.")), EndNode(3)];

    private static readonly NodeNumbering _nodes = NodeNumbering.Of(_graph);
    private static readonly SpeakerNumbering _speakers = SpeakerNumbering.Of(_graph);

    [Fact]
    public void Write_ANode_SitsWhereItsPositionSaysAndNotWhereItsIdDid()
    {
        Assert.Equal(1, Write(EndNode(3)).Id);
    }

    [Fact]
    public void Write_ALine_NamesItsSpeakerByPosition()
    {
        var written = Assert.IsType<LineNode>(
            Write(LineNode(12, _alice, Text("Hello."))));

        Assert.Equal(0, written.Speaker);
        AssertSays(written.Speech, "Hello.");
        Assert.Null(written.Condition);
    }

    [Fact]
    public void Write_AConditionalLine_KeepsWhatMustHold()
    {
        var line = ConditionalLineNode(12, _alice, Condition("IsCurious"), Text("Hello."));

        Assert.NotNull(Assert.IsType<LineNode>(Write(line)).Condition);
    }

    [Fact]
    public void Write_AChoice_SaysWhetherItsOptionsMustKeepTheirOrder()
    {
        Assert.True(Assert.IsType<ChoiceNode>(Write(ChoiceNode(12, ordered: true))).Ordered);
        Assert.False(Assert.IsType<ChoiceNode>(Write(ChoiceNode(12))).Ordered);
    }

    [Fact]
    public void Write_ARandomChoice_CarriesNothingOfItsOwn()
    {
        Assert.IsType<RandomChoiceNode>(Write(RandomChoiceNode(12)));
    }

    [Fact]
    public void Write_ABranch_CarriesNothingOfItsOwn()
    {
        Assert.IsType<BranchNode>(Write(BranchNode(12)));
    }

    [Fact]
    public void Write_AControlNode_KeepsTheEffectsItRuns()
    {
        var written = Assert.IsType<ControlNode>(
            Write(ControlNode(12, DefaultCommand("wait"), Query("Key"))));

        Assert.Collection(
            written.Effects,
            first => AssertCommands(first, "wait"),
            second => AssertQueries(second, "Key"));
    }

    [Fact]
    public void Write_AnEnd_LeadsNowhere()
    {
        Assert.Empty(Assert.IsType<EndNode>(Write(EndNode(3))).Out);
    }

    [Fact]
    public void Write_ANodesWaysOut_AreWrittenWithIt()
    {
        var choice = ChoiceNode(12, false, OptionEdge(NodeId(3), "that one"));

        var written = Assert.IsType<ChoiceNode>(Write(choice));

        AssertSays(AssertLeadsTo<PlaybookEdges.OptionEdge>(Assert.Single(written.Out), 1).Label, "that one");
    }

    [Fact]
    public void Write_EveryNodeTheGraphHas_HasASample()
    {
        MappingAssert.AssertCoversEveryMember<GraphNodes.DialogueNode>(Samples());
    }

    [Fact]
    public void Write_NoNodeAtAll_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => NodeMapping.Write(null!, _nodes, _speakers));
    }

    [Fact]
    public void Write_NoNumberingAtAll_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => NodeMapping.Write(EndNode(3), null!, _speakers));
        Assert.Throws<ArgumentNullException>(() => NodeMapping.Write(EndNode(3), _nodes, null!));
    }

    private static Node Write(GraphNodes.DialogueNode node) =>
        NodeMapping.Write(node, _nodes, _speakers);

    private static IReadOnlyList<GraphNodes.DialogueNode> Samples() =>
    [
        LineNode(12, _alice),
        ChoiceNode(12),
        RandomChoiceNode(12),
        BranchNode(12),
        ControlNode(12),
        EndNode(3),
    ];
}

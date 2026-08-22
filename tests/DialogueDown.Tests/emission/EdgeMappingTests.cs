using DialogueDown.Emission;
using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Weights;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstFactory;
using static DialogueDown.Tests.Support.DialogueGraphFactory;
using static DialogueDown.Tests.Support.PlaybookEdgeAssert;
using static DialogueDown.Tests.Support.SpeechAssert;
using GraphEdges = DialogueDown.Graph.Edges;

namespace DialogueDown.Tests.Emission;

public sealed class EdgeMappingTests
{
    // Ids that are not positions, so a target copied straight off the graph would be wrong.
    private static readonly NodeNumbering _numbering = NodeNumbering.Of([EndNode(12), EndNode(3)]);

    [Fact]
    public void Write_AnEdge_NamesItsTargetByPosition()
    {
        AssertLeadsTo<SuccessionEdge>(Write(SuccessionEdge(NodeId(3))), 1);
    }

    [Fact]
    public void Write_AnOption_KeepsItsLabelAndCondition()
    {
        var option = OptionEdge(NodeId(3), "Ask about the inn", Condition("IsCurious"));

        var written = AssertLeadsTo<OptionEdge>(Write(option), 1);

        AssertSays(written.Label, "Ask about the inn");
        Assert.NotNull(written.Condition);
    }

    [Fact]
    public void Write_ARandomOption_KeepsItsWeight()
    {
        var written = AssertLeadsTo<RandomOptionEdge>(
            Write(RandomOptionEdge(NodeId(12), NumberWeight(25.0))), 0);

        Assert.Equal(25.0, Assert.IsType<NumberWeight>(written.Weight).Percentage);
        Assert.Null(written.Condition);
    }

    [Fact]
    public void Write_ABranchArm_KeepsTheOrderItIsTriedIn()
    {
        var written = AssertLeadsTo<BranchEdge>(
            Write(BranchEdge(NodeId(3), order: 2, Condition("IsBrave"))), 1);

        Assert.Equal(2, written.Order);
    }

    [Fact]
    public void Write_ADivert_KeepsWhatTheWriterCalledIt()
    {
        var written = AssertLeadsTo<DivertEdge>(Write(DivertEdge(NodeId(3), "the inn")), 1);

        AssertSays(written.Label, "the inn");
    }

    [Fact]
    public void Write_EveryEdgeTheGraphHas_HasASample()
    {
        MappingAssert.AssertCoversEveryMember<GraphEdges.Edge>(Samples());
    }

    [Fact]
    public void Write_EveryWayOutOfANode_KeepsTheirOrder()
    {
        var edges = EdgeMapping.Write(
            [SuccessionEdge(NodeId(12)), SuccessionEdge(NodeId(3))], _numbering);

        AssertLeadTo(edges, 0, 1);
    }

    [Fact]
    public void Write_NoEdgeAtAll_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(
            () => EdgeMapping.Write((GraphEdges.Edge)null!, _numbering));
    }

    [Fact]
    public void Write_NoNumbering_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(
            () => EdgeMapping.Write(SuccessionEdge(NodeId(3)), null!));
    }

    private static Edge Write(GraphEdges.Edge edge) => EdgeMapping.Write(edge, _numbering);

    private static IReadOnlyList<GraphEdges.Edge> Samples() =>
    [
        SuccessionEdge(NodeId(3)),
        OptionEdge(NodeId(3)),
        RandomOptionEdge(NodeId(3)),
        BranchEdge(NodeId(3)),
        DivertEdge(NodeId(3)),
    ];
}

using DialogueDown.Emission;
using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Weights;
using DialogueDown.TestSupport;
using static DialogueDown.Tests.Support.DialogueAstFactory;
using static DialogueDown.Tests.Support.DialogueGraphFactory;
using static DialogueDown.Tests.Support.PlaybookEdgeAssert;
using static DialogueDown.Tests.Support.SpeechAssert;
using GraphEdges = DialogueDown.Graph.Edges;

namespace DialogueDown.Tests.Emission;

public sealed class EdgeMappingTests
{
    // The playbook names a node by its position, not its graph id. Each id here differs from its
    // position, so a target copied straight off the graph would be wrong.
    private const int FirstId = 12;
    private const int FirstPosition = 0;
    private const int SecondId = 3;
    private const int SecondPosition = 1;

    private static readonly NodeNumbering _numbering =
        NodeNumbering.Of([EndNode(FirstId), EndNode(SecondId)]);

    [Fact]
    public void Write_AnEdge_NamesItsTargetByPosition()
    {
        AssertLeadsTo<SuccessionEdge>(Write(SuccessionEdge(NodeId(SecondId))), SecondPosition);
    }

    [Fact]
    public void Write_AnOption_KeepsItsLabelAndCondition()
    {
        var option = OptionEdge(NodeId(SecondId), "Ask about the inn", Condition("IsCurious"));

        var written = AssertLeadsTo<OptionEdge>(Write(option), SecondPosition);

        AssertSays(written.Label, "Ask about the inn");
        Assert.NotNull(written.Condition);
    }

    [Fact]
    public void Write_AnOption_HasTheExtraSpacesOfItsLabelCollapsed()
    {
        var written = AssertLeadsTo<OptionEdge>(
            Write(OptionEdge(NodeId(SecondId), "Ask  about the inn ")), SecondPosition);

        AssertSays(written.Label, "Ask about the inn");
    }

    [Fact]
    public void Write_ARandomOption_KeepsItsWeight()
    {
        var written = AssertLeadsTo<RandomOptionEdge>(
            Write(RandomOptionEdge(NodeId(FirstId), NumberWeight(25.0))), FirstPosition);

        Assert.Equal(25.0, Assert.IsType<NumberWeight>(written.Weight).Percentage);
        Assert.Null(written.Condition);
    }

    [Fact]
    public void Write_ABranch_KeepsItsCondition()
    {
        var written = AssertLeadsTo<BranchEdge>(
            Write(BranchEdge(NodeId(SecondId), Condition("IsBrave"))), SecondPosition);

        Assert.NotNull(written.Condition);
    }

    [Fact]
    public void Write_ADivert_KeepsWhatTheWriterCalledIt()
    {
        var written = AssertLeadsTo<DivertEdge>(
            Write(DivertEdge(NodeId(SecondId), "the inn")), SecondPosition);

        AssertSays(written.Label, "the inn");
    }

    [Fact]
    public void Write_ADivert_HasTheExtraSpacesOfItsLabelCollapsed()
    {
        var written = AssertLeadsTo<DivertEdge>(
            Write(DivertEdge(NodeId(SecondId), " Back  home ")), SecondPosition);

        AssertSays(written.Label, "Back home");
    }

    [Fact]
    public void Write_EveryEdgeTheGraphHas_HasASample()
    {
        UnionCoverageAssert.AssertCoversEveryMember<GraphEdges.Edge>(Samples());
    }

    [Fact]
    public void Write_EveryWayOutOfANode_KeepsTheirOrder()
    {
        var edges = EdgeMapping.Write(
            [SuccessionEdge(NodeId(FirstId)), SuccessionEdge(NodeId(SecondId))], _numbering);

        AssertLeadTo(edges, FirstPosition, SecondPosition);
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
            () => EdgeMapping.Write(SuccessionEdge(NodeId(SecondId)), null!));
    }

    private static Edge Write(GraphEdges.Edge edge) => EdgeMapping.Write(edge, _numbering);

    private static IReadOnlyList<GraphEdges.Edge> Samples() =>
    [
        SuccessionEdge(NodeId(SecondId)),
        OptionEdge(NodeId(SecondId)),
        RandomOptionEdge(NodeId(SecondId)),
        BranchEdge(NodeId(SecondId)),
        DivertEdge(NodeId(SecondId)),
    ];
}

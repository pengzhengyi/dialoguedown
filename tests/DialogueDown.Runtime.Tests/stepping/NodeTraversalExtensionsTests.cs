using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Runtime.Stepping;
using static DialogueDown.Runtime.Tests.PlaybookNodes;

namespace DialogueDown.Runtime.Tests.Stepping;

/// <summary>
/// Which way out of a node a run takes.
/// </summary>
public sealed class NodeTraversalExtensionsTests
{
    [Fact]
    public void SuccessionTarget_ANodeThatCarriesOn_IsWhereItCarriesOnTo()
    {
        var node = Line(0, speaker: 0, "Hello.", next: 7);

        Assert.Equal(7, node.SuccessionTarget());
    }

    [Fact]
    public void OnwardTarget_ANodeCarryingAJump_LeadsWhereTheJumpGoes()
    {
        // The jump is the way out the writer asked for. The succession beside it is where the run
        // would have landed had the jump not applied, so taking it here would be reading past the
        // jump rather than through it.
        var node = new ControlNode(
            0, [], Condition: null, [new DivertEdge(9, [], Condition: null), new SuccessionEdge(4)]);

        Assert.Equal(9, node.OnwardTarget());
    }

    [Fact]
    public void OnwardTarget_ANodeWithOnlyAFallThrough_LeadsWhereItFallsThrough()
    {
        var node = Line(0, speaker: 0, "Hello.", next: 7);

        Assert.Equal(7, node.OnwardTarget());
    }

    [Fact]
    public void SuccessionTarget_ANodeWithNoWayOut_IsNowhere()
    {
        Assert.Null(new EndNode(0).SuccessionTarget());
    }

    [Fact]
    public void SuccessionTarget_ANodeLeavingByAnotherKindOfEdge_IsNowhere()
    {
        // An option is taken by choosing it, not by carrying on, so nothing here leads onward.
        var node = new ChoiceNode(0, Ordered: true, [new OptionEdge(1, [], null)]);

        Assert.Null(node.SuccessionTarget());
    }

    [Fact]
    public void SuccessionTarget_ANodeAmongOtherWaysOut_IsTheOneThatCarriesOn()
    {
        var node = new ControlNode(0, [], Condition: null, [new DivertEdge(9, [], null), new SuccessionEdge(4)]);

        Assert.Equal(4, node.SuccessionTarget());
    }

    [Fact]
    public void SuccessionTarget_ANodeFallingThroughTwoWays_IsRefused()
    {
        // A reader refuses such a node, so a run never meets one. Reading the single succession
        // rather than the first of however many is what keeps that guarantee load-bearing.
        var node = new ControlNode(0, [], Condition: null, [new SuccessionEdge(4), new SuccessionEdge(9)]);

        Assert.Throws<InvalidOperationException>(() => node.SuccessionTarget());
    }

    [Fact]
    public void SuccessionTarget_NoNode_IsRefused()
    {
        Assert.Throws<ArgumentNullException>(() => ((Node)null!).SuccessionTarget());
    }
}

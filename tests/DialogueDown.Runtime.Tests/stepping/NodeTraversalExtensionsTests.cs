using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Runtime.Stepping;

namespace DialogueDown.Runtime.Tests.Stepping;

/// <summary>
/// Which way out of a node a run takes.
/// </summary>
public sealed class NodeTraversalExtensionsTests
{
    [Fact]
    public void SuccessionTarget_ANodeThatCarriesOn_IsWhereItCarriesOnTo()
    {
        var node = Playbooks.Line(0, speaker: 0, "Hello.", next: 7);

        Assert.Equal(7, node.SuccessionTarget());
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
    public void SuccessionTarget_NoNode_IsRefused()
    {
        Assert.Throws<ArgumentNullException>(() => ((Node)null!).SuccessionTarget());
    }
}

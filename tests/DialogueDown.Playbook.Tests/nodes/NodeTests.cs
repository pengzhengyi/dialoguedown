using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Tests.Support;
namespace DialogueDown.Playbook.Tests.Nodes;

public sealed class NodeTests
{
    [Fact]
    public void EveryNodeKind_IsTaggedAndRegistered()
    {
        UnionAssert.AssertEveryMemberIsTagged<Node>(typeof(NodeKinds));
    }

    [Fact]
    public void Equality_EqualWaysOut_AreEqual()
    {
        var left = new ChoiceNode(0, false, [new SuccessionEdge(1)]);
        var right = new ChoiceNode(0, false, [new SuccessionEdge(1)]);

        Assert.True(left == right);
        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentWaysOut_AreNotEqual()
    {
        var left = new ChoiceNode(0, false, [new SuccessionEdge(1)]);
        var right = new ChoiceNode(0, false, [new SuccessionEdge(2)]);

        Assert.False(left == right);
    }

    [Fact]
    public void Equality_DifferentNodeKinds_AreNotEqual()
    {
        // Two kinds may share an id and ways out; the kind still separates them.
        Node choice = new ChoiceNode(0, false, []);
        Node branch = new BranchNode(0, []);

        Assert.NotEqual(choice, branch);
    }
}

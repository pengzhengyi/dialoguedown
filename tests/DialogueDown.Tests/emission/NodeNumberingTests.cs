using DialogueDown.Emission;
using static DialogueDown.Tests.Support.DialogueGraphFactory;

namespace DialogueDown.Tests.Emission;

public sealed class NodeNumberingTests
{
    [Fact]
    public void Position_ANodeInTheList_IsWhereItSits()
    {
        var numbering = NodeNumbering.Of([EndNode(4), EndNode(1), EndNode(9)]);

        Assert.Equal(0, numbering.Position(NodeId(4)));
        Assert.Equal(1, numbering.Position(NodeId(1)));
        Assert.Equal(2, numbering.Position(NodeId(9)));
    }

    [Fact]
    public void Position_IdsThatWereNeverPositions_AreNumberedAnyway()
    {
        // The compiler mints ids as blocks are encountered, so they need not run 0, 1, 2 in list
        // order — which is the whole reason writing a playbook renumbers.
        var numbering = NodeNumbering.Of([EndNode(12), EndNode(3)]);

        Assert.Equal(1, numbering.Position(NodeId(3)));
    }

    [Fact]
    public void Position_ANodeThatIsNotInTheGraph_IsRejected()
    {
        var numbering = NodeNumbering.Of([EndNode(0)]);

        var error = Assert.Throws<ArgumentException>(() => numbering.Position(NodeId(6)));

        Assert.Contains("6", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Of_NoNodesAtAll_NumbersNothing()
    {
        var numbering = NodeNumbering.Of([]);

        Assert.Throws<ArgumentException>(() => numbering.Position(NodeId(0)));
    }

    [Fact]
    public void Of_NoListAtAll_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => NodeNumbering.Of(null!));
    }
}

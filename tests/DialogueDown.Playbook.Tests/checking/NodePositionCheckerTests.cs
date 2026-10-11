using DialogueDown.Playbook.Checking;
using DialogueDown.TestSupport;
using static DialogueDown.TestSupport.PlaybookNodeFactory;

namespace DialogueDown.Playbook.Tests.Checking;

public sealed class NodePositionCheckerTests
{
    private readonly NodePositionChecker _checker = new();

    [Fact]
    public void Check_EveryNodeAtItsOwnIndex_IsAccepted()
    {
        var playbook = PlaybookFactory.Document(nodes: [End(id: 0), End(id: 1)]);

        _checker.Check(playbook);
    }

    [Fact]
    public void Check_ANodeClaimingSomebodyElsesIndex_NamesBoth()
    {
        // An id is the position, so a node claiming another one silently redirects every edge
        // that ever names it.
        var playbook = PlaybookFactory.Document(nodes: [End(id: 0), End(id: 7)]);

        var error = Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));

        Assert.Equal("Node at index 1 claims id 7; a node's id is its position.", error.Message);
    }

    [Fact]
    public void Check_NoNodesAtAll_IsAccepted()
    {
        // Nothing here is misnumbered. Whether a playbook may be empty is the entry's question,
        // not this one's.
        var playbook = PlaybookFactory.Document(nodes: []);

        _checker.Check(playbook);
    }

    [Fact]
    public void Check_NothingAtAll_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => _checker.Check(null!));
    }
}

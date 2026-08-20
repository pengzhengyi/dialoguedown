using DialogueDown.Playbook.Tests.Support;

namespace DialogueDown.Playbook.Tests.Checking;

public sealed class NodePositionCheckerTests
{
    private readonly NodePositionChecker _checker = new();

    [Fact]
    public void Check_EveryNodeAtItsOwnIndex_IsAccepted()
    {
        var playbook = PlaybookFactory.Document(nodes: [new EndNode(0), new EndNode(1)]);

        _checker.Check(playbook);
    }

    [Fact]
    public void Check_ANodeClaimingSomebodyElsesIndex_NamesBoth()
    {
        // An id is the position, so a node claiming another one silently redirects every edge
        // that ever names it.
        var playbook = PlaybookFactory.Document(nodes: [new EndNode(0), new EndNode(7)]);

        var error = Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));

        Assert.Contains("1", error.Message, StringComparison.Ordinal);
        Assert.Contains("7", error.Message, StringComparison.Ordinal);
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

using DialogueDown.Playbook.Checking;
using DialogueDown.TestSupport;
using static DialogueDown.TestSupport.PlaybookNodeFactory;

namespace DialogueDown.Playbook.Tests.Checking;

public sealed class ReferenceCheckerTests
{
    private readonly ReferenceChecker _checker = new();

    [Fact]
    public void Check_APlaybookWhereEveryReferenceIsInRange_IsAccepted()
    {
        var playbook = PlaybookFactory.Document(
            entry: 0,
            anchors: [("the-inn", 1)],
            speakers: [PlaybookFactory.Speaker()],
            nodes: [SilentLine(next: 1), End(id: 1)]);

        _checker.Check(playbook);
    }

    [Fact]
    public void Check_AnEntryLeadingNowhere_IsRefused()
    {
        var playbook = PlaybookFactory.Document(entry: 4);

        var error = Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));

        Assert.Equal("The entry points at 4, but there are only 1.", error.Message);
    }

    [Fact]
    public void Check_APlaybookWithNoNodesAtAll_IsRefused()
    {
        // With no nodes, the entry points at nothing. This is where an empty playbook is caught.
        var playbook = PlaybookFactory.Document(nodes: []);

        var error = Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));

        Assert.Equal("The entry points at 0, but there are only 0.", error.Message);
    }

    [Fact]
    public void Check_AnEdgeLeadingNowhere_NamesTheNodeItLeaves()
    {
        var playbook = PlaybookFactory.Document(
            speakers: [PlaybookFactory.Speaker()],
            nodes: [SilentLine(next: 9), End(id: 1)]);

        var error = Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));

        Assert.Equal("An edge out of node 0 points at 9, but there are only 2.", error.Message);
    }

    [Fact]
    public void Check_ALineQuotingASpeakerWhoIsNotThere_IsRefused()
    {
        var playbook = PlaybookFactory.Document(
            nodes: [SilentLine(speaker: 3, next: 0)]);

        var error = Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));

        Assert.Equal("The speaker of node 0 points at 3, but there are only 0.", error.Message);
    }

    [Fact]
    public void Check_AnAnchorLeadingNowhere_NamesTheSlug()
    {
        var playbook = PlaybookFactory.Document(anchors: [("the-inn", 5)]);

        var error = Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));

        Assert.Equal("The anchor 'the-inn' points at 5, but there are only 1.", error.Message);
    }

    [Fact]
    public void Check_NothingAtAll_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => _checker.Check(null!));
    }
}

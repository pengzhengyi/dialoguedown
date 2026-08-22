using DialogueDown.Playbook.Checking;
using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Tests.Support;
namespace DialogueDown.Playbook.Tests.Checking;

public sealed class ReferenceCheckerTests
{
    private readonly ReferenceChecker _checker = new();

    [Fact]
    public void Check_APlaybookWhereEveryReferenceLands_IsAccepted()
    {
        var playbook = PlaybookFactory.Document(
            entry: 0,
            anchors: [("the-inn", 1)],
            speakers: [PlaybookFactory.Speaker()],
            nodes: [Line(0, speaker: 0, to: 1), new EndNode(1)]);

        _checker.Check(playbook);
    }

    [Fact]
    public void Check_AnEntryLeadingNowhere_IsRefused()
    {
        var playbook = PlaybookFactory.Document(entry: 4);

        var error = Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));

        Assert.Contains("entry", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("4", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Check_APlaybookWithNoNodesAtAll_IsRefused()
    {
        // Nothing to play, so the entry cannot land. This is where an empty playbook is caught.
        var playbook = PlaybookFactory.Document(nodes: []);

        Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));
    }

    [Fact]
    public void Check_AnEdgeLeadingNowhere_NamesTheNodeItLeaves()
    {
        var playbook = PlaybookFactory.Document(
            speakers: [PlaybookFactory.Speaker()],
            nodes: [Line(0, speaker: 0, to: 9), new EndNode(1)]);

        var error = Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));

        Assert.Contains("9", error.Message, StringComparison.Ordinal);
        Assert.Contains("edge", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Check_ALineQuotingASpeakerWhoIsNotThere_IsRefused()
    {
        var playbook = PlaybookFactory.Document(nodes: [Line(0, speaker: 3, to: 0)]);

        var error = Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));

        Assert.Contains("speaker", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Check_AnAnchorLeadingNowhere_NamesTheSlug()
    {
        var playbook = PlaybookFactory.Document(anchors: [("the-inn", 5)]);

        var error = Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));

        Assert.Contains("the-inn", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Check_NothingAtAll_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => _checker.Check(null!));
    }

    private static LineNode Line(int id, int speaker, int to) =>
        new(id, speaker, [], null, [new SuccessionEdge(to)]);
}

using DialogueDown.Playbook.Checking;
using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.TestSupport;
using static DialogueDown.TestSupport.PlaybookNodeFactory;

namespace DialogueDown.Playbook.Tests.Checking;

public sealed class OutwardShapeCheckerTests
{
    private readonly OutwardShapeChecker _checker = new();

    [Fact]
    public void Check_APlaybookWhereEveryNodeIsWellShaped_IsAccepted()
    {
        var playbook = PlaybookFactory.Document(
            speakers: [PlaybookFactory.Speaker()],
            nodes:
            [
                SilentLine(id: 0, ways: [new SuccessionEdge(1)]),
                Choice(
                    id: 1,
                    Option("Go east", target: 2),
                    Option("Go west", condition: "HasMap", target: 2),
                    new SuccessionEdge(2)),
                End(id: 2),
            ]);

        _checker.Check(playbook);
    }

    [Fact]
    public void Check_AnEnd_CarriesNoEdgesAndIsAccepted()
    {
        // An end's record takes no edges, so this is all an end can be; the schema guards a
        // hand-written end that carries an `out`.
        _checker.Check(PlaybookFactory.Document(nodes: [End(id: 0)]));
    }

    [Fact]
    public void Check_ALineCarryingAnOption_IsRefused()
    {
        var playbook = PlaybookFactory.Document(
            speakers: [PlaybookFactory.Speaker()],
            nodes: [SilentLine(ways: [Option("Go east", target: 1)]), End(id: 1)]);

        var error = Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));

        Assert.Equal(
            "Node 0, a line, carries a 'option' way out; its arms are 'divert' edges.",
            error.Message);
    }

    [Fact]
    public void Check_AChoiceMixingOptionAndBranchArms_IsRefused()
    {
        var playbook = PlaybookFactory.Document(
            nodes: [Choice(Option("Go east", target: 1), Else(target: 1)), End(id: 1)]);

        var error = Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));

        Assert.Equal(
            "Node 0, a choice, carries a 'branch' way out; its arms are 'option' edges.",
            error.Message);
    }

    [Fact]
    public void Check_ALineWithTwoDiverts_IsRefused()
    {
        var playbook = PlaybookFactory.Document(
            speakers: [PlaybookFactory.Speaker()],
            nodes: [SilentLine(ways: [Divert(target: 1), Divert(target: 1)]), End(id: 1)]);

        var error = Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));

        Assert.Equal("Node 0, a line, carries 2 'divert' edges; it takes at most 1.", error.Message);
    }

    [Fact]
    public void Check_AChoiceWithNoOptions_IsRefused()
    {
        var playbook = PlaybookFactory.Document(
            nodes: [Choice(new SuccessionEdge(1)), End(id: 1)]);

        var error = Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));

        Assert.Equal("Node 0, a choice, carries 0 arms; it needs at least 1.", error.Message);
    }

    [Fact]
    public void Check_ANodeWithTwoSuccessions_IsRefused()
    {
        var playbook = PlaybookFactory.Document(
            speakers: [PlaybookFactory.Speaker()],
            nodes: [SilentLine(ways: [new SuccessionEdge(1), new SuccessionEdge(1)]), End(id: 1)]);

        var error = Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));

        Assert.Equal(
            "Node 0 carries 2 succession edges; a node falls through at most one way.",
            error.Message);
    }

    [Fact]
    public void Check_AnAllGatedChoiceWithNoSuccession_LeadsNowhere_AndIsRefused()
    {
        var playbook = PlaybookFactory.Document(
            nodes:
            [
                Choice(
                    Option("Go east", condition: "HasMap", target: 1),
                    Option("Go west", condition: "HasMap", target: 1)),
                End(id: 1),
            ]);

        var error = Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));

        Assert.Equal(
            "Node 0, a choice, can lead nowhere: nothing guarantees a way out, "
                + "and it has no succession to fall through to.",
            error.Message);
    }

    [Fact]
    public void Check_AnIfWithNoElseAndNoSuccession_LeadsNowhere_AndIsRefused()
    {
        var playbook = PlaybookFactory.Document(
            nodes: [Branch(Arm("HasMap", target: 1)), End(id: 1)]);

        var error = Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));

        Assert.Equal(
            "Node 0, a branch, can lead nowhere: nothing guarantees a way out, "
                + "and it has no succession to fall through to.",
            error.Message);
    }

    [Fact]
    public void Check_AConditionalLineWithAnOpenDivertButNoSuccession_LeadsNowhere_AndIsRefused()
    {
        // The divert always applies, but the line itself may be skipped, so there must be a
        // succession to fall through to when it is.
        var playbook = PlaybookFactory.Document(
            speakers: [PlaybookFactory.Speaker()],
            nodes: [SilentLine(condition: "HasMap", ways: [Divert(target: 1)]), End(id: 1)]);

        var error = Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));

        Assert.Equal(
            "Node 0, a line, can lead nowhere: nothing guarantees a way out, "
                + "and it has no succession to fall through to.",
            error.Message);
    }

    [Fact]
    public void Check_APlainLineWithNoWayOut_LeadsNowhere_AndIsRefused()
    {
        var playbook = PlaybookFactory.Document(
            speakers: [PlaybookFactory.Speaker()],
            nodes: [SilentLine(), End(id: 1)]);

        var error = Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));

        Assert.Equal(
            "Node 0, a line, can lead nowhere: nothing guarantees a way out, "
                + "and it has no succession to fall through to.",
            error.Message);
    }

    [Fact]
    public void Check_AnAllGatedChoiceWithASuccession_IsAccepted()
    {
        var playbook = PlaybookFactory.Document(
            nodes:
            [
                Choice(
                    Option("Go east", condition: "HasMap", target: 1),
                    Option("Go west", condition: "HasMap", target: 1),
                    new SuccessionEdge(1)),
                End(id: 1),
            ]);

        _checker.Check(playbook);
    }

    [Fact]
    public void Check_AConditionalLineWithAGatedDivertAndASuccession_IsAccepted()
    {
        var playbook = PlaybookFactory.Document(
            speakers: [PlaybookFactory.Speaker()],
            nodes:
            [
                SilentLine(
                    condition: "HasMap",
                    ways: [Divert(condition: "HasMap", target: 1), new SuccessionEdge(1)]),
                End(id: 1),
            ]);

        _checker.Check(playbook);
    }

    [Fact]
    public void Check_AChoiceWithAnOpenOptionAndARedundantSuccession_IsAccepted()
    {
        // The succession can never run — an open option is always there — but it plays no
        // differently, so it is not refused.
        var playbook = PlaybookFactory.Document(
            nodes: [Choice(Option("Go east", target: 1), new SuccessionEdge(1)), End(id: 1)]);

        _checker.Check(playbook);
    }

    [Fact]
    public void Check_ARandomChoiceWhereEveryArmIsGated_AndNoSuccession_IsRefused()
    {
        var playbook = PlaybookFactory.Document(
            nodes: [RandomChoice(EvenChance(condition: "HasMap", target: 1)), End(id: 1)]);

        var error = Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));

        Assert.Equal(
            "Node 0, a random-choice, can lead nowhere: nothing guarantees a way out, "
                + "and it has no succession to fall through to.",
            error.Message);
    }

    [Fact]
    public void Check_AControlWithAnOpenDivert_IsAccepted()
    {
        var playbook = PlaybookFactory.Document(
            nodes: [DivertingUnlabeled(target: 1), End(id: 1)]);

        _checker.Check(playbook);
    }

    [Fact]
    public void Check_ARandomChoiceWithAnOpenArm_IsAccepted()
    {
        var playbook = PlaybookFactory.Document(
            nodes: [RandomChoice(EvenChance(target: 1)), End(id: 1)]);

        _checker.Check(playbook);
    }

    [Fact]
    public void Check_ABranchWithAnElseArm_IsAccepted()
    {
        // The else arm carries no condition, so the branch always resolves and needs no succession.
        var playbook = PlaybookFactory.Document(
            nodes: [Branch(Arm("HasMap", target: 1), Else(target: 1)), End(id: 1)]);

        _checker.Check(playbook);
    }

    [Fact]
    public void EveryConcreteNodeKind_HasARowInTheShapeRule()
    {
        var kinds = UnionMembers.Of<Node>().ToArray();

        // If this fails, a node kind was added: give it a shape in NodeShape.For and add accept
        // and refuse cases here.
        Assert.Equal(6, kinds.Length);
    }

    [Fact]
    public void Check_Null_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => _checker.Check(null!));
    }
}

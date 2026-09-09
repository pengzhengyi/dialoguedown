using DialogueDown.Playbook.Checking;
using DialogueDown.Playbook.Conditions;
using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Tests.Support;
using DialogueDown.Playbook.Weights;

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
                new LineNode(0, 0, [], null, [new SuccessionEdge(1)]),
                new ChoiceNode(1, false, [Option(2), Option(2, gated: true), new SuccessionEdge(2)]),
                new EndNode(2),
            ]);

        _checker.Check(playbook);
    }

    [Fact]
    public void Check_AnEnd_CarriesNoEdgesAndIsAccepted()
    {
        // An end's record takes no edges, so this is all an end can be; the schema guards a
        // hand-written end that carries an `out`.
        _checker.Check(PlaybookFactory.Document(nodes: [new EndNode(0)]));
    }

    [Fact]
    public void Check_ALineCarryingAnOption_IsRefused()
    {
        var playbook = PlaybookFactory.Document(
            speakers: [PlaybookFactory.Speaker()],
            nodes: [new LineNode(0, 0, [], null, [Option(1)]), new EndNode(1)]);

        var error = Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));

        Assert.Contains("option", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("divert", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Check_AChoiceMixingOptionAndBranchArms_IsRefused()
    {
        var playbook = PlaybookFactory.Document(
            nodes: [new ChoiceNode(0, false, [Option(1), new BranchEdge(1, 0, null)]), new EndNode(1)]);

        Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));
    }

    [Fact]
    public void Check_ALineWithTwoDiverts_IsRefused()
    {
        var playbook = PlaybookFactory.Document(
            speakers: [PlaybookFactory.Speaker()],
            nodes: [new LineNode(0, 0, [], null, [Divert(1), Divert(1)]), new EndNode(1)]);

        var error = Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));

        Assert.Contains("divert", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Check_AChoiceWithNoOptions_IsRefused()
    {
        var playbook = PlaybookFactory.Document(
            nodes: [new ChoiceNode(0, false, [new SuccessionEdge(1)]), new EndNode(1)]);

        Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));
    }

    [Fact]
    public void Check_ANodeWithTwoSuccessions_IsRefused()
    {
        var playbook = PlaybookFactory.Document(
            speakers: [PlaybookFactory.Speaker()],
            nodes: [new LineNode(0, 0, [], null, [new SuccessionEdge(1), new SuccessionEdge(1)]), new EndNode(1)]);

        var error = Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));

        Assert.Contains("succession", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Check_AnAllGatedChoiceWithNoSuccession_LeadsNowhere_AndIsRefused()
    {
        var playbook = PlaybookFactory.Document(
            nodes:
            [
                new ChoiceNode(0, false, [Option(1, gated: true), Option(1, gated: true)]),
                new EndNode(1),
            ]);

        var error = Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));

        Assert.Contains("nowhere", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Check_AnIfWithNoElseAndNoSuccession_LeadsNowhere_AndIsRefused()
    {
        var playbook = PlaybookFactory.Document(
            nodes: [new BranchNode(0, [new BranchEdge(1, 0, Key())]), new EndNode(1)]);

        Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));
    }

    [Fact]
    public void Check_AConditionalLineWithAnOpenDivertButNoSuccession_LeadsNowhere_AndIsRefused()
    {
        // The divert always applies, but the line itself may be skipped, so there must be a
        // succession to fall through to when it is.
        var playbook = PlaybookFactory.Document(
            speakers: [PlaybookFactory.Speaker()],
            nodes: [new LineNode(0, 0, [], Key(), [Divert(1)]), new EndNode(1)]);

        Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));
    }

    [Fact]
    public void Check_APlainLineWithNoWayOut_LeadsNowhere_AndIsRefused()
    {
        var playbook = PlaybookFactory.Document(
            speakers: [PlaybookFactory.Speaker()],
            nodes: [new LineNode(0, 0, [], null, []), new EndNode(1)]);

        Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));
    }

    [Fact]
    public void Check_AnAllGatedChoiceWithASuccession_IsAccepted()
    {
        var playbook = PlaybookFactory.Document(
            nodes:
            [
                new ChoiceNode(0, false, [Option(1, gated: true), Option(1, gated: true), new SuccessionEdge(1)]),
                new EndNode(1),
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
                new LineNode(0, 0, [], Key(), [Divert(1, gated: true), new SuccessionEdge(1)]),
                new EndNode(1),
            ]);

        _checker.Check(playbook);
    }

    [Fact]
    public void Check_AChoiceWithAnOpenOptionAndARedundantSuccession_IsAccepted()
    {
        // The succession can never run — an open option is always there — but it plays no
        // differently, so it is not refused.
        var playbook = PlaybookFactory.Document(
            nodes: [new ChoiceNode(0, false, [Option(1), new SuccessionEdge(1)]), new EndNode(1)]);

        _checker.Check(playbook);
    }

    [Fact]
    public void Check_ARandomChoiceWhereEveryArmIsGated_AndNoSuccession_IsRefused()
    {
        var playbook = PlaybookFactory.Document(
            nodes:
            [
                new RandomChoiceNode(0, [new RandomOptionEdge(1, new AutoWeight(), Key())]),
                new EndNode(1),
            ]);

        Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));
    }

    [Fact]
    public void Check_AControlWithAnOpenDivert_IsAccepted()
    {
        var playbook = PlaybookFactory.Document(
            nodes: [new ControlNode(0, [], null, [Divert(1)]), new EndNode(1)]);

        _checker.Check(playbook);
    }

    [Fact]
    public void Check_ARandomChoiceWithAnOpenArm_IsAccepted()
    {
        var playbook = PlaybookFactory.Document(
            nodes:
            [
                new RandomChoiceNode(0, [new RandomOptionEdge(1, new AutoWeight(), null)]),
                new EndNode(1),
            ]);

        _checker.Check(playbook);
    }

    [Fact]
    public void Check_ABranchWithAnElseArm_IsAccepted()
    {
        // The else arm carries no condition, so the branch always resolves and needs no succession.
        var playbook = PlaybookFactory.Document(
            nodes:
            [
                new BranchNode(0, [new BranchEdge(1, 0, Key()), new BranchEdge(1, 1, null)]),
                new EndNode(1),
            ]);

        _checker.Check(playbook);
    }

    [Fact]
    public void EveryConcreteNodeKind_HasARowInTheShapeRule()
    {
        var kinds = typeof(Node).Assembly.GetTypes()
            .Where(type => type.IsSubclassOf(typeof(Node)) && !type.IsAbstract)
            .ToArray();

        // If this fails, a node kind was added: give it a row in OutwardShapeChecker.ShapeOf and
        // accept/refuse cases here.
        Assert.Equal(6, kinds.Length);
    }

    [Fact]
    public void Check_Null_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => _checker.Check(null!));
    }

    private static KeyCondition Key() => new("HasMap");

    private static OptionEdge Option(int to, bool gated = false) =>
        new(to, [], gated ? Key() : null);

    private static DivertEdge Divert(int to, bool gated = false) =>
        new(to, [], gated ? Key() : null);
}

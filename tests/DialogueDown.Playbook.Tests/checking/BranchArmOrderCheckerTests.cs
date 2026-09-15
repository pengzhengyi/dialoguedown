using DialogueDown.Playbook.Checking;
using DialogueDown.Playbook.Conditions;
using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Tests.Support;

namespace DialogueDown.Playbook.Tests.Checking;

public sealed class BranchArmOrderCheckerTests
{
    private readonly BranchArmOrderChecker _checker = new();

    [Fact]
    public void Check_ABranchWhoseArmsAscendWithTheElseLast_IsAccepted()
    {
        var playbook = PlaybookFactory.Document(
            nodes:
            [
                new BranchNode(0, [Arm(1, 0), Arm(1, 1), Arm(1, 2, isElse: true)]),
                new EndNode(1),
            ]);

        _checker.Check(playbook);
    }

    [Fact]
    public void Check_ABranchWithASingleGatedArm_IsAccepted()
    {
        // One gated arm is trivially in order.
        var playbook = PlaybookFactory.Document(
            nodes: [new BranchNode(0, [Arm(1, 0)]), new EndNode(1)]);

        _checker.Check(playbook);
    }

    [Fact]
    public void Check_ABranchWhoseOnlyArmIsAnElse_IsRefused()
    {
        // An else falls back from the gated arms; with none, the branch is no condition at all.
        var playbook = PlaybookFactory.Document(
            nodes: [new BranchNode(0, [Arm(1, 0, isElse: true)]), new EndNode(1)]);

        var error = Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));

        Assert.Contains("gated", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Check_ABranchWhoseOrdersHaveAGap_IsAccepted()
    {
        var playbook = PlaybookFactory.Document(
            nodes: [new BranchNode(0, [Arm(1, 0), Arm(1, 2, isElse: true)]), new EndNode(1)]);

        _checker.Check(playbook);
    }

    [Fact]
    public void Check_ABranchWithASuccessionAmongTheArms_IsAccepted()
    {
        // A fall-through is not an arm, so its position is the outward-shape rule's business.
        var playbook = PlaybookFactory.Document(
            nodes:
            [
                new BranchNode(0, [new SuccessionEdge(1), Arm(1, 0), Arm(1, 1, isElse: true)]),
                new EndNode(1),
            ]);

        _checker.Check(playbook);
    }

    [Fact]
    public void Check_ABranchWithNoArms_IsIgnored()
    {
        // An arm count of zero is the outward-shape rule's to refuse; this rule has nothing to order.
        var playbook = PlaybookFactory.Document(
            nodes: [new BranchNode(0, []), new EndNode(1)]);

        _checker.Check(playbook);
    }

    [Fact]
    public void Check_ABranchWhoseArmsAreOutOfOrder_IsRefused()
    {
        var playbook = PlaybookFactory.Document(
            nodes: [new BranchNode(0, [Arm(1, 1), Arm(1, 0, isElse: true)]), new EndNode(1)]);

        var error = Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));

        Assert.Contains("order", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Check_ABranchWithTwoArmsSharingAnOrder_IsRefused()
    {
        var playbook = PlaybookFactory.Document(
            nodes: [new BranchNode(0, [Arm(1, 0), Arm(1, 0, isElse: true)]), new EndNode(1)]);

        Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));
    }

    [Fact]
    public void Check_ABranchWhoseElseIsNotLast_IsRefused()
    {
        // The orders ascend, but the conditionless arm still comes before a gated one.
        var playbook = PlaybookFactory.Document(
            nodes: [new BranchNode(0, [Arm(1, 0, isElse: true), Arm(1, 1)]), new EndNode(1)]);

        var error = Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));

        Assert.Contains("else", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Check_ABranchWithTwoConditionlessArms_IsRefused()
    {
        var playbook = PlaybookFactory.Document(
            nodes:
            [
                new BranchNode(0, [Arm(1, 0, isElse: true), Arm(1, 1, isElse: true)]),
                new EndNode(1),
            ]);

        Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));
    }

    [Fact]
    public void Check_ANonBranchNode_IsIgnored()
    {
        // A line is not a branch, so its edges are left to the outward-shape rule.
        var playbook = PlaybookFactory.Document(
            speakers: [PlaybookFactory.Speaker()],
            nodes:
            [
                new LineNode(0, 0, [], null, [new BranchEdge(1, 5, null), new BranchEdge(1, 2, null)]),
                new EndNode(1),
            ]);

        _checker.Check(playbook);
    }

    [Fact]
    public void Check_Null_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => _checker.Check(null!));
    }

    private static KeyCondition Key() => new("HasMap");

    private static BranchEdge Arm(int to, int order, bool isElse = false) =>
        new(to, order, isElse ? null : Key());
}

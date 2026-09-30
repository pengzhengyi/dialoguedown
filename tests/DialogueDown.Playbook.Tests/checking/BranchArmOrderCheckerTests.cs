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
    public void Check_ABranchWhoseElseIsLast_IsAccepted()
    {
        var playbook = PlaybookFactory.Document(
            nodes: [new BranchNode(0, [Gated(1), Gated(1), Else(1)]), new EndNode(1)]);

        _checker.Check(playbook);
    }

    [Fact]
    public void Check_ABranchWithASingleGatedArm_IsAccepted()
    {
        var playbook = PlaybookFactory.Document(
            nodes: [new BranchNode(0, [Gated(1)]), new EndNode(1)]);

        _checker.Check(playbook);
    }

    [Fact]
    public void Check_ABranchWhoseOnlyArmIsAnElse_IsRefused()
    {
        // An else falls back from the gated arms; with none, the branch is no condition at all.
        var playbook = PlaybookFactory.Document(
            nodes: [new BranchNode(0, [Else(1)]), new EndNode(1)]);

        var error = Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));

        Assert.Contains("gated", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Check_ABranchWithASuccessionAmongTheArms_IsAccepted()
    {
        // A fall-through is not an arm, so its position is the outward-shape rule's business.
        var playbook = PlaybookFactory.Document(
            nodes: [new BranchNode(0, [new SuccessionEdge(1), Gated(1), Else(1)]), new EndNode(1)]);

        _checker.Check(playbook);
    }

    [Fact]
    public void Check_ABranchWithNoArms_IsIgnored()
    {
        // An arm count of zero is the outward-shape rule's to refuse; this rule has nothing to check.
        var playbook = PlaybookFactory.Document(
            nodes: [new BranchNode(0, []), new EndNode(1)]);

        _checker.Check(playbook);
    }

    [Fact]
    public void Check_ABranchWhoseElseIsNotLast_IsRefused()
    {
        var playbook = PlaybookFactory.Document(
            nodes: [new BranchNode(0, [Else(1), Gated(1)]), new EndNode(1)]);

        var error = Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));

        Assert.Contains("else", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Check_ABranchWithASecondElse_IsRefused()
    {
        // Only one arm can be last, so the first else comes before another arm.
        var playbook = PlaybookFactory.Document(
            nodes: [new BranchNode(0, [Gated(1), Else(1), Else(1)]), new EndNode(1)]);

        var error = Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));

        Assert.Contains("else", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Check_ANonBranchNode_IsIgnored()
    {
        // A line is not a branch, so its edges are left to the outward-shape rule.
        var playbook = PlaybookFactory.Document(
            speakers: [PlaybookFactory.Speaker()],
            nodes: [new LineNode(0, 0, [], null, [Else(1), Gated(1)]), new EndNode(1)]);

        _checker.Check(playbook);
    }

    [Fact]
    public void Check_Null_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => _checker.Check(null!));
    }

    private static BranchEdge Gated(int to) => new(to, new KeyCondition("HasMap"));

    private static BranchEdge Else(int to) => new(to, Condition: null);
}

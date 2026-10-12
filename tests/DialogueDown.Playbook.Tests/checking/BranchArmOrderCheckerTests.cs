using DialogueDown.Playbook.Checking;
using DialogueDown.TestSupport;
using static DialogueDown.TestSupport.PlaybookNodeFactory;

namespace DialogueDown.Playbook.Tests.Checking;

public sealed class BranchArmOrderCheckerTests
{
    private readonly BranchArmOrderChecker _checker = new();

    [Fact]
    public void Check_ABranchWhoseElseIsLast_IsAccepted()
    {
        var playbook = PlaybookFactory.Document(
            nodes:
            [
                Branch(Arm("HasMap", target: 1), Arm("HasMap", target: 1), Else(target: 1)),
                End(id: 1),
            ]);

        _checker.Check(playbook);
    }

    [Fact]
    public void Check_ABranchWithASingleGatedArm_IsAccepted()
    {
        var playbook = PlaybookFactory.Document(
            nodes: [Branch(Arm("HasMap", target: 1)), End(id: 1)]);

        _checker.Check(playbook);
    }

    [Fact]
    public void Check_ABranchWhoseOnlyArmIsAnElse_IsRefused()
    {
        // An else is the fallback for gated arms; with no gated arm, the branch tests nothing.
        var playbook = PlaybookFactory.Document(
            nodes: [Branch(Else(target: 1)), End(id: 1)]);

        var error = Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));

        Assert.Equal(
            "Node 0, a branch, carries no gated arm; every arm is an else. "
                + "A branch is a block condition, so at least one arm carries one.",
            error.Message);
    }

    [Fact]
    public void Check_ABranchWithASuccessionAmongTheArms_IsAccepted()
    {
        // A fall-through is not an arm, so its position is the outward-shape rule's business.
        var playbook = PlaybookFactory.Document(
            nodes:
            [
                Branch(Succession(1), Arm("HasMap", target: 1), Else(target: 1)),
                End(id: 1),
            ]);

        _checker.Check(playbook);
    }

    [Fact]
    public void Check_ABranchWithNoArms_IsIgnored()
    {
        // An arm count of zero is the outward-shape rule's to refuse; this rule has nothing to check.
        var playbook = PlaybookFactory.Document(
            nodes: [Branch(), End(id: 1)]);

        _checker.Check(playbook);
    }

    [Fact]
    public void Check_ABranchWhoseElseIsNotLast_IsRefused()
    {
        var playbook = PlaybookFactory.Document(
            nodes: [Branch(Else(target: 1), Arm("HasMap", target: 1)), End(id: 1)]);

        var error = Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));

        Assert.Equal(
            "Node 0, a branch, carries its else arm before another arm; the else is tried last.",
            error.Message);
    }

    [Fact]
    public void Check_ABranchWithASecondElse_IsRefused()
    {
        // Only one arm can be last, so the first else comes before another arm.
        var playbook = PlaybookFactory.Document(
            nodes:
            [
                Branch(Arm("HasMap", target: 1), Else(target: 1), Else(target: 1)),
                End(id: 1),
            ]);

        var error = Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));

        Assert.Equal(
            "Node 0, a branch, carries its else arm before another arm; the else is tried last.",
            error.Message);
    }

    [Fact]
    public void Check_ANonBranchNode_IsIgnored()
    {
        // A line is not a branch, so its edges are left to the outward-shape rule.
        var playbook = PlaybookFactory.Document(
            speakers: [PlaybookFactory.Speaker()],
            nodes: [SilentLine(ways: [Else(target: 1), Arm("HasMap", target: 1)]), End(id: 1)]);

        _checker.Check(playbook);
    }

    [Fact]
    public void Check_Null_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => _checker.Check(null!));
    }
}

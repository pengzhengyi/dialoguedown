using DialogueDown.Playbook.Checking;
using DialogueDown.TestSupport;
using static DialogueDown.TestSupport.PlaybookNodeFactory;

namespace DialogueDown.Playbook.Tests.Checking;

public sealed class BranchOrderCheckerTests
{
    private readonly BranchOrderChecker _checker = new();

    [Fact]
    public void Check_ABranchWhoseElseIsLast_IsAccepted()
    {
        var playbook = PlaybookFactory.Document(
            nodes:
            [
                Branch(Gated("HasMap", target: 1), Gated("HasMap", target: 1), Else(target: 1)),
                End(id: 1),
            ]);

        _checker.Check(playbook);
    }

    [Fact]
    public void Check_ABranchWithASingleGatedBranch_IsAccepted()
    {
        var playbook = PlaybookFactory.Document(
            nodes: [Branch(Gated("HasMap", target: 1)), End(id: 1)]);

        _checker.Check(playbook);
    }

    [Fact]
    public void Check_ABranchWhoseOnlyBranchIsAnElse_IsRefused()
    {
        // An else falls back from gated branches; with none, the branch node tests nothing.
        var playbook = PlaybookFactory.Document(
            nodes: [Branch(Else(target: 1)), End(id: 1)]);

        var error = Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));

        Assert.Equal(
            "Node 0, a branch, carries no gated branch; every branch is an else. "
                + "A branch node is a block condition, so at least one branch carries a condition.",
            error.Message);
    }

    [Fact]
    public void Check_ABranchWithASuccessionAmongTheBranches_IsAccepted()
    {
        // A fall-through is not a branch, so its position is the outward-shape rule's business.
        var playbook = PlaybookFactory.Document(
            nodes:
            [
                Branch(Succession(1), Gated("HasMap", target: 1), Else(target: 1)),
                End(id: 1),
            ]);

        _checker.Check(playbook);
    }

    [Fact]
    public void Check_ABranchWithNoBranches_IsIgnored()
    {
        // A branch count of zero is the outward-shape rule's to refuse; this rule has nothing to check.
        var playbook = PlaybookFactory.Document(
            nodes: [Branch(), End(id: 1)]);

        _checker.Check(playbook);
    }

    [Fact]
    public void Check_ABranchWhoseElseIsNotLast_IsRefused()
    {
        var playbook = PlaybookFactory.Document(
            nodes: [Branch(Else(target: 1), Gated("HasMap", target: 1)), End(id: 1)]);

        var error = Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));

        Assert.Equal(
            "Node 0, a branch, carries its else before another branch; the else is tried last.",
            error.Message);
    }

    [Fact]
    public void Check_ABranchWithASecondElse_IsRefused()
    {
        // Only one branch can be last, so the first else comes before another branch.
        var playbook = PlaybookFactory.Document(
            nodes:
            [
                Branch(Gated("HasMap", target: 1), Else(target: 1), Else(target: 1)),
                End(id: 1),
            ]);

        var error = Assert.Throws<InvalidPlaybookException>(() => _checker.Check(playbook));

        Assert.Equal(
            "Node 0, a branch, carries its else before another branch; the else is tried last.",
            error.Message);
    }

    [Fact]
    public void Check_ANonBranchNode_IsIgnored()
    {
        // A line is not a branch, so its edges are left to the outward-shape rule.
        var playbook = PlaybookFactory.Document(
            speakers: [PlaybookFactory.Speaker()],
            nodes: [SilentLine(ways: [Else(target: 1), Gated("HasMap", target: 1)]), End(id: 1)]);

        _checker.Check(playbook);
    }

    [Fact]
    public void Check_Null_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => _checker.Check(null!));
    }
}

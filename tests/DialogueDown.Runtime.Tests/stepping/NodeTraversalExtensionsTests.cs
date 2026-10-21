using DialogueDown.Playbook.Nodes;
using DialogueDown.Runtime.Stepping;
using static DialogueDown.Runtime.Tests.World;
using static DialogueDown.TestSupport.PlaybookNodeFactory;

namespace DialogueDown.Runtime.Tests.Stepping;

/// <summary>
/// Which way out of a node a run takes.
/// </summary>
public sealed class NodeTraversalExtensionsTests
{
    [Fact]
    public void SuccessionTarget_ANodeThatCarriesOn_IsWhereItCarriesOnTo()
    {
        var node = Line("Hello.", id: 0, next: 7);

        Assert.Equal(7, node.SuccessionTarget());
    }

    [Fact]
    public void OnwardTarget_ANodeCarryingAJump_LeadsWhereTheJumpGoes()
    {
        // The jump is the way out the writer asked for; the succession beside it is only for when
        // the jump does not apply.
        var node = ControlOf([], id: 0, next: 4, ways: [Divert(target: 9)]);

        Assert.Equal(9, node.OnwardTarget());
    }

    [Fact]
    public void OnwardTarget_ANodeWithOnlyAFallThrough_LeadsWhereItFallsThrough()
    {
        var node = Line("Hello.", id: 0, next: 7);

        Assert.Equal(7, node.OnwardTarget());
    }

    [Fact]
    public void OnwardTarget_AJumpTheWorldAllows_IsTheWayOutTaken() =>
        Assert.Equal(9, AJumpTheWorldMustAllow().OnwardTarget(Answering(("Alice.HasKey", true))));

    [Fact]
    public void OnwardTarget_AJumpTheWorldWithholds_FallsThroughBeneathIt() =>
        Assert.Equal(4, AJumpTheWorldMustAllow().OnwardTarget(Answering(("Alice.HasKey", false))));

    [Fact]
    public void OnwardTarget_AJumpTheWorldWithholdsAndNothingBeneathIt_IsNowhere() =>
        Assert.Null(
            ControlOf([], id: 0, ways: [Divert("Alice.HasKey", target: 9)])
                .OnwardTarget(Answering(("Alice.HasKey", false))));

    [Fact]
    public void OnwardTarget_AnUnguardedJump_IsTakenWhateverTheWorldAnswered() =>
        Assert.Equal(
            9,
            ControlOf([], id: 0, next: 4, ways: [Divert(target: 9)])
                .OnwardTarget(Answering(("Alice.HasKey", false))));

    [Fact]
    public void OnwardTarget_ABranchWhoseBranchesBothHold_TakesTheFirst() =>
        // The branches are tried in the order written, so a later branch that also holds is never reached.
        Assert.Equal(
            7, ABranchWithAnElse().OnwardTarget(Answering(("Alice.HasKey", true), ("Alice.HasPick", true))));

    [Fact]
    public void OnwardTarget_ABranchWhoseFirstBranchIsWithheld_TakesTheNextTheWorldAllows() =>
        Assert.Equal(
            8, ABranchWithAnElse().OnwardTarget(Answering(("Alice.HasKey", false), ("Alice.HasPick", true))));

    [Fact]
    public void OnwardTarget_ABranchWhoseBranchesAreAllWithheld_TakesItsElse() =>
        Assert.Equal(
            9, ABranchWithAnElse().OnwardTarget(Answering(("Alice.HasKey", false), ("Alice.HasPick", false))));

    [Fact]
    public void OnwardTarget_ABranchWithoutAnElseWhoseBranchIsWithheld_FallsThroughBeneathIt() =>
        // The succession beneath a block leads past it, so a block with no branch taken is skipped.
        Assert.Equal(4, ABranchWithoutAnElse().OnwardTarget(Answering(("Alice.HasKey", false))));

    [Fact]
    public void OnwardTarget_ABranchWhoseBranchIsWithheldAndNothingBeneathIt_IsNowhere() =>
        Assert.Null(
            Branch(Gated("Alice.HasKey", target: 7))
                .OnwardTarget(Answering(("Alice.HasKey", false))));

    [Fact]
    public void OnwardTarget_WithoutAnswers_TakesABranchNothingGuards() =>
        Assert.Equal(9, Branch(Else(target: 9)).OnwardTarget());

    [Fact]
    public void OnwardTarget_WithoutAnswers_PassesOverAJumpTheWorldMustAllow() =>
        // Nobody asked the world about the jump, so nothing says it fires.
        Assert.Equal(4, AJumpTheWorldMustAllow().OnwardTarget());

    [Fact]
    public void OnwardTarget_WithAnswersInHand_IsNotReadFromNothing() =>
        Assert.Throws<ArgumentNullException>(
            () => ((Node)null!).OnwardTarget(Answering(("Alice.HasKey", true))));

    [Fact]
    public void OnwardTarget_IsNotReadWithoutAnswers() =>
        Assert.Throws<ArgumentNullException>(() => End(id: 0).OnwardTarget(null!));

    [Fact]
    public void SuccessionTarget_ANodeWithNoWayOut_IsNowhere()
    {
        Assert.Null(End(id: 0).SuccessionTarget());
    }

    [Fact]
    public void SuccessionTarget_ANodeLeavingByAnotherKindOfEdge_IsNowhere()
    {
        // An option is taken by choosing it, not by carrying on, so nothing here leads onward.
        var node = NumberedChoice(Option("Go east", target: 1));

        Assert.Null(node.SuccessionTarget());
    }

    [Fact]
    public void SuccessionTarget_ANodeAmongOtherWaysOut_IsTheOneThatCarriesOn()
    {
        var node = ControlOf([], id: 0, next: 4, ways: [Divert(target: 9)]);

        Assert.Equal(4, node.SuccessionTarget());
    }

    [Fact]
    public void SuccessionTarget_ANodeFallingThroughTwoWays_IsRefused()
    {
        // A reader refuses such a node, so a run never meets one. Throwing here, rather than taking
        // the first succession, shows when that guarantee breaks.
        var node = ControlOf([], id: 0, ways: [Succession(4), Succession(9)]);

        Assert.Throws<InvalidOperationException>(() => node.SuccessionTarget());
    }

    [Fact]
    public void SuccessionTarget_NoNode_IsRefused()
    {
        Assert.Throws<ArgumentNullException>(() => ((Node)null!).SuccessionTarget());
    }

    [Fact]
    public void Options_AMenuWithAFallThrough_AreOnlyItsOptionsInTheOrderListed()
    {
        var menu = Choice(Option("Go east", target: 7), Succession(4), Option("Go west", target: 8));

        Assert.Equal([7, 8], menu.Options().Select(option => option.Target));
    }

    [Fact]
    public void Options_NoMenu_IsRefused()
    {
        Assert.Throws<ArgumentNullException>(() => ((ChoiceNode)null!).Options());
    }

    [Fact]
    public void RandomOptions_ARandomChoiceWithAFallThrough_AreOnlyItsRandomOptionsInTheOrderListed()
    {
        var random = RandomChoice(Chance(50, target: 7), Succession(4), EvenChance(target: 8));

        Assert.Equal([7, 8], random.RandomOptions().Select(option => option.Target));
    }

    [Fact]
    public void RandomOptions_NoRandomChoice_IsRefused()
    {
        Assert.Throws<ArgumentNullException>(() => ((RandomChoiceNode)null!).RandomOptions());
    }

    /// <summary>A node whose jump the world must allow, with a fall-through beneath it.</summary>
    /// <remarks>
    /// <code>
    /// node 0 -- jumps to node 9 when Alice.HasKey, falls through to node 4 when it does not
    /// </code>
    /// </remarks>
    /// <returns>The node.</returns>
    private static ControlNode AJumpTheWorldMustAllow() =>
        ControlOf([], id: 0, next: 4, ways: [Divert("Alice.HasKey", target: 9)]);

    /// <summary>A block condition with an if, an elseif, and an else.</summary>
    /// <remarks>
    /// <code>
    /// node 0 -- to node 7 when Alice.HasKey, else to node 8 when Alice.HasPick, else to node 9
    /// </code>
    /// </remarks>
    /// <returns>The node.</returns>
    private static BranchNode ABranchWithAnElse() =>
        Branch(Gated("Alice.HasKey", target: 7), Gated("Alice.HasPick", target: 8), Else(target: 9));

    /// <summary>A block condition with an if and no else, and a succession beneath it.</summary>
    /// <remarks>
    /// <code>
    /// node 0 -- to node 7 when Alice.HasKey, falls through to node 4 when it does not
    /// </code>
    /// </remarks>
    /// <returns>The node.</returns>
    private static BranchNode ABranchWithoutAnElse() =>
        Branch(Gated("Alice.HasKey", target: 7), Succession(4));
}

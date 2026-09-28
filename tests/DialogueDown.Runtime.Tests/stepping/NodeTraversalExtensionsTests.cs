using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Runtime.Stepping;
using static DialogueDown.Runtime.Tests.PlaybookNodes;
using static DialogueDown.Runtime.Tests.World;

namespace DialogueDown.Runtime.Tests.Stepping;

/// <summary>
/// Which way out of a node a run takes.
/// </summary>
public sealed class NodeTraversalExtensionsTests
{
    [Fact]
    public void SuccessionTarget_ANodeThatCarriesOn_IsWhereItCarriesOnTo()
    {
        var node = Line(0, speaker: 0, "Hello.", next: 7);

        Assert.Equal(7, node.SuccessionTarget());
    }

    [Fact]
    public void OnwardTarget_ANodeCarryingAJump_LeadsWhereTheJumpGoes()
    {
        // The jump is the way out the writer asked for. The succession beside it is where the run
        // would have landed had the jump not applied, so taking it here would be reading past the
        // jump rather than through it.
        var node = Bare(0, Divert(9), new SuccessionEdge(4));

        Assert.Equal(9, node.OnwardTarget());
    }

    [Fact]
    public void OnwardTarget_ANodeWithOnlyAFallThrough_LeadsWhereItFallsThrough()
    {
        var node = Line(0, speaker: 0, "Hello.", next: 7);

        Assert.Equal(7, node.OnwardTarget());
    }

    [Fact]
    public void OnwardTarget_AJumpTheWorldAllows_IsTheWayOutTaken() =>
        Assert.Equal(9, AJumpTheWorldMustAllow().OnwardTarget(Answering(("Alice.HasKey", true))));

    [Fact]
    public void OnwardTarget_AJumpTheWorldWithholds_FallsThroughBeneathIt() =>
        // A jump nobody allowed is not a way out, so the succession written beneath it is what the
        // writer left the run to land on.
        Assert.Equal(4, AJumpTheWorldMustAllow().OnwardTarget(Answering(("Alice.HasKey", false))));

    [Fact]
    public void OnwardTarget_AJumpTheWorldWithholdsAndNothingBeneathIt_IsNowhere() =>
        Assert.Null(
            Bare(0, Divert(9, "Alice.HasKey"))
                .OnwardTarget(Answering(("Alice.HasKey", false))));

    [Fact]
    public void OnwardTarget_AnUnguardedJump_IsTakenWhateverTheWorldAnswered() =>
        Assert.Equal(
            9,
            Bare(0, Divert(9), new SuccessionEdge(4))
                .OnwardTarget(Answering(("Alice.HasKey", false))));

    [Fact]
    public void OnwardTarget_ABranchWhoseArmsBothHold_TakesTheFirst() =>
        // The arms are tried in the order written, so a later arm that also holds is never reached.
        Assert.Equal(
            7, ABranchWithAnElse().OnwardTarget(Answering(("Alice.HasKey", true), ("Alice.HasPick", true))));

    [Fact]
    public void OnwardTarget_ABranchWhoseFirstArmIsWithheld_TakesTheNextTheWorldAllows() =>
        Assert.Equal(
            8, ABranchWithAnElse().OnwardTarget(Answering(("Alice.HasKey", false), ("Alice.HasPick", true))));

    [Fact]
    public void OnwardTarget_ABranchWhoseArmsAreAllWithheld_TakesItsElse() =>
        Assert.Equal(
            9, ABranchWithAnElse().OnwardTarget(Answering(("Alice.HasKey", false), ("Alice.HasPick", false))));

    [Fact]
    public void OnwardTarget_ABranchWithoutAnElseWhoseArmIsWithheld_FallsThroughBeneathIt() =>
        // The succession beneath a block leads past it, so a block with no arm taken is skipped.
        Assert.Equal(4, ABranchWithoutAnElse().OnwardTarget(Answering(("Alice.HasKey", false))));

    [Fact]
    public void OnwardTarget_ABranchWhoseArmIsWithheldAndNothingBeneathIt_IsNowhere() =>
        Assert.Null(
            Branch(0, Arm(7, order: 0, "Alice.HasKey"))
                .OnwardTarget(Answering(("Alice.HasKey", false))));

    [Fact]
    public void OnwardTarget_WithoutAnswers_TakesAnArmNothingGuards() =>
        Assert.Equal(9, Branch(0, Else(9, order: 0)).OnwardTarget());

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
        Assert.Throws<ArgumentNullException>(() => End(0).OnwardTarget(null!));

    [Fact]
    public void SuccessionTarget_ANodeWithNoWayOut_IsNowhere()
    {
        Assert.Null(End(0).SuccessionTarget());
    }

    [Fact]
    public void SuccessionTarget_ANodeLeavingByAnotherKindOfEdge_IsNowhere()
    {
        // An option is taken by choosing it, not by carrying on, so nothing here leads onward.
        var node = new ChoiceNode(0, Ordered: true, [new OptionEdge(1, [], null)]);

        Assert.Null(node.SuccessionTarget());
    }

    [Fact]
    public void SuccessionTarget_ANodeAmongOtherWaysOut_IsTheOneThatCarriesOn()
    {
        var node = Bare(0, Divert(9), new SuccessionEdge(4));

        Assert.Equal(4, node.SuccessionTarget());
    }

    [Fact]
    public void SuccessionTarget_ANodeFallingThroughTwoWays_IsRefused()
    {
        // A reader refuses such a node, so a run never meets one. Reading the single succession
        // rather than the first of however many is what keeps that guarantee load-bearing.
        var node = Bare(0, new SuccessionEdge(4), new SuccessionEdge(9));

        Assert.Throws<InvalidOperationException>(() => node.SuccessionTarget());
    }

    [Fact]
    public void SuccessionTarget_NoNode_IsRefused()
    {
        Assert.Throws<ArgumentNullException>(() => ((Node)null!).SuccessionTarget());
    }

    /// <summary>A node whose jump the world must allow, with a fall-through beneath it.</summary>
    /// <remarks>
    /// <code>
    /// node 0 -- jumps to node 9 when Alice.HasKey, falls through to node 4 when it does not
    /// </code>
    /// </remarks>
    /// <returns>The node.</returns>
    private static ControlNode AJumpTheWorldMustAllow() =>
        Bare(0, Divert(9, "Alice.HasKey"), new SuccessionEdge(4));

    /// <summary>A block condition with an if, an elseif, and an else.</summary>
    /// <remarks>
    /// <code>
    /// node 0 -- to node 7 when Alice.HasKey, else to node 8 when Alice.HasPick, else to node 9
    /// </code>
    /// </remarks>
    /// <returns>The node.</returns>
    private static BranchNode ABranchWithAnElse() =>
        Branch(
            0,
            Arm(7, order: 0, "Alice.HasKey"),
            Arm(8, order: 1, "Alice.HasPick"),
            Else(9, order: 2));

    /// <summary>A block condition with an if and no else, and a succession beneath it.</summary>
    /// <remarks>
    /// <code>
    /// node 0 -- to node 7 when Alice.HasKey, falls through to node 4 when it does not
    /// </code>
    /// </remarks>
    /// <returns>The node.</returns>
    private static BranchNode ABranchWithoutAnElse() =>
        Branch(0, Arm(7, order: 0, "Alice.HasKey"), new SuccessionEdge(4));
}

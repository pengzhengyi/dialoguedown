using DialogueDown.Graph.Builder;
using DialogueDown.Graph.Passes;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Graph.Passes;

public sealed class BlockSequenceTests
{
    [Fact]
    public void AllContinuations_FlatSequence_ChainsEachBlockThenReachesTheContinuation()
    {
        // n0 ▶ n1 ▶ End.
        Assert.Equal(
            [(0, 1), (1, 2)],
            Walk("""
                Alice: one

                Bob: two
                """));
    }

    [Fact]
    public void AllContinuations_EmptySequence_YieldsNothing() =>
        Assert.Empty(Walk(""));

    [Fact]
    public void AllContinuations_Choice_GivesEveryArmTheChoicesOwnContinuation()
    {
        // n0 question, n1 choice, n2/n3 the arms, n4 what follows, n5 End. Both arms continue
        // at n4, and the choice does too, so picking either weaves back to the same place.
        Assert.Equal(
            [(0, 1), (1, 4), (2, 4), (3, 4), (4, 5)],
            Walk("""
                Guide: Pick.

                - Alice: Left.

                - Alice: Right.

                Guide: Done.
                """));
    }

    [Fact]
    public void AllContinuations_ArmWithSeveralBlocks_ChainsInsideTheArmBeforeWeavingBack()
    {
        // n1 the choice, n2 ▶ n3 within the arm, then n3 weaves back to n4.
        Assert.Equal(
            [(0, 1), (1, 4), (2, 3), (3, 4), (4, 5)],
            Walk("""
                Guide: Pick.

                - Alice: Left.

                  Alice: Onward.

                Guide: Done.
                """));
    }

    [Fact]
    public void AllContinuations_NestedChoice_WeavesTheInnerArmsPastTheOuterOne()
    {
        // n1 outer choice, n2 its arm, n3 the inner choice, n4/n5 the inner arms — all of which
        // continue at n6, where the outer body itself continues.
        Assert.Equal(
            [(0, 1), (1, 6), (2, 3), (3, 6), (4, 6), (5, 6), (6, 7)],
            Walk("""
                Guide: Pick.

                - Alice: Outer.

                  - Alice: Inner one.

                  - Alice: Inner two.

                Guide: Done.
                """));
    }

    [Fact]
    public void AllContinuations_ConditionalBlock_GivesEveryBranchTheBlocksOwnContinuation()
    {
        // n0 the block, n1/n2 the branch bodies, n3 what follows, n4 End. Both branches continue
        // at n3, and the block does too, so taking any of them resumes at the same place.
        Assert.Equal(
            [(0, 3), (1, 3), (2, 3), (3, 4)],
            Walk("""
                > `if` `"Rich"?`
                >
                > Alice: Upstairs.
                >
                > `else`
                >
                > Alice: Side door.

                Guide: Done.
                """));
    }

    [Fact]
    public void AllContinuations_BranchWithSeveralBlocks_ChainsInsideTheBranchBeforeResuming()
    {
        // n1 ▶ n2 within the branch, then n2 resumes at n3 where the block itself continues.
        Assert.Equal(
            [(0, 3), (1, 2), (2, 3), (3, 4)],
            Walk("""
                > `if` `"Rich"?`
                >
                > Alice: Upstairs.
                >
                > Alice: Mind the step.

                Guide: Done.
                """));
    }

    [Fact]
    public void EntryOf_BodyWithBlocks_LeadsToItsFirstBlock()
    {
        var (draft, context) = Lowered("""
            Alice: one

            Bob: two
            """);

        Assert.Equal(
            draft.IdOf(context.TopLevelBlocks[0]),
            BlockSequence.EntryOf(context.TopLevelBlocks, draft.End, draft));
    }

    [Fact]
    public void EntryOf_EmptyBody_LeadsToTheContinuationInstead()
    {
        var (draft, _) = Lowered("Alice: one");

        Assert.Equal(draft.End, BlockSequence.EntryOf([], draft.End, draft));
    }

    // Each pair is (the block's node id, the node control reaches once that block is done), so a
    // test reads the walk as plain numbers.
    private static IReadOnlyList<(int Block, int Next)> Walk(string source)
    {
        var (draft, context) = Lowered(source);

        return [.. BlockSequence
            .AllContinuations(context.TopLevelBlocks, draft.End, draft)
            .Select(step => (draft.IdOf(step.Block).Value, step.Continuation.Value))];
    }

    // Node creation assigns every block its id and adds the End, which the walk resolves against.
    private static (GraphDraft Draft, GraphBuildContext Context) Lowered(string source)
    {
        var draft = GraphDraftFactory.Draft();
        var context = GraphBuildContextFactory.Context(source);
        new NodeCreationPass().Apply(draft, context);
        return (draft, context);
    }
}

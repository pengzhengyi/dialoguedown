using DialogueDown.Graph.Builder;
using DialogueDown.Graph.Passes;
using DialogueDown.Script.Ast;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Graph.Passes;

public sealed class BlockSequenceTests
{
    [Fact]
    public void WithSuccessors_FlatSequence_ChainsEachBlockThenReachesTheContinuation()
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
    public void WithSuccessors_EmptySequence_YieldsNothing() =>
        Assert.Empty(Walk(""));

    [Fact]
    public void WithSuccessors_Choice_GivesEveryArmTheChoicesOwnContinuation()
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
    public void WithSuccessors_ArmWithSeveralBlocks_ChainsInsideTheArmBeforeWeavingBack()
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
    public void WithSuccessors_NestedChoice_WeavesTheInnerArmsPastTheOuterOne()
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

    // Each pair is (the block's node id, the node control reaches once that block is done), so a
    // test reads the walk as plain numbers. Node creation assigns the ids and the End node.
    private static IReadOnlyList<(int Block, int Next)> Walk(string source)
    {
        var draft = GraphDraftFactory.Draft();
        var context = GraphBuildContextFactory.Context(source);
        new NodeCreationPass().Apply(draft, context);

        return [.. BlockSequence
            .WithSuccessors(context.Blocks, draft.End, draft)
            .Select(step => (draft.IdOf(step.Block).Value, step.Next.Value))];
    }
}

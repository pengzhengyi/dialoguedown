using DialogueDown.Graph.Builder;
using DialogueDown.Graph.Edges;
using DialogueDown.Script.Ast;

namespace DialogueDown.Graph.Passes;

/// <summary>
/// Fans a conditional block out into its branches: one edge per branch, leading to the first node
/// of that branch's body and carrying both the guard that decides whether the branch is taken and
/// the order it is tried in. Runs after node creation, and before succession — which then gives
/// the block a fall-through only when every branch is guarded, since a block with no <c>else</c>
/// is skipped whole when none of its conditions hold.
/// </summary>
internal sealed class BranchPass : GraphBuildPass
{
    protected override void ApplyCore(GraphDraft draft, GraphBuildContext context)
    {
        foreach (var (block, continuation) in
                 BlockSequence.AllContinuations(context.TopLevelBlocks, draft.End, draft))
        {
            if (block is ControlBlock conditional)
            {
                FanOut(conditional, continuation, draft);
            }
        }
    }

    private static void FanOut(ControlBlock conditional, NodeId continuation, GraphDraft draft)
    {
        var node = draft.IdOf(conditional);
        foreach (var (branch, order) in
                 conditional.Branches.Select((branch, order) => (branch, order)))
        {
            draft.AddEdge(
                node,
                new BranchEdge(
                    BlockSequence.EntryOf(branch.Body, continuation, draft),
                    order,
                    branch.Condition));
        }
    }
}

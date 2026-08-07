using DialogueDown.Graph.Builder;
using DialogueDown.Graph.Edges;

namespace DialogueDown.Graph.Passes;

/// <summary>
/// Wires the default flow: each block falls through to the node control reaches once it is done —
/// the next block in its sequence, or that sequence's continuation. Runs after the passes that add
/// diverts and options, so a node control already leaves is not also given a fall-through. A
/// guarded node is the exception: its guard may skip it whole, taking any divert it holds with it,
/// so it needs somewhere to continue however it leaves when it does play.
/// </summary>
internal sealed class SuccessionPass : GraphBuildPass
{
    protected override void ApplyCore(GraphDraft draft, GraphBuildContext context)
    {
        foreach (var (block, continuation) in
                 BlockSequence.AllContinuations(context.TopLevelBlocks, draft.End, draft))
        {
            var source = draft.IdOf(block);
            if (!draft.Node(source).LeavesUnconditionally())
            {
                draft.AddEdge(source, new SuccessionEdge(continuation));
            }
        }
    }
}

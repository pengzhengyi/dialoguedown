using DialogueDown.Graph.Builder;
using DialogueDown.Script.Ast;

namespace DialogueDown.Graph.Passes;

/// <summary>
/// Wires the default flow: within a sequence of blocks each one falls through to the next, and the
/// last falls through to the sequence's <b>continuation</b> — where control resumes once the
/// sequence is exhausted. The document is one sequence continuing to the End node. Runs after
/// diverts, so a node that already leaves unconditionally is left to terminate rather than also
/// falling through.
/// </summary>
internal sealed class SuccessionPass : GraphBuildPass
{
    protected override void ApplyCore(GraphDraft draft, GraphBuildContext context) =>
        Chain(context.Blocks, draft.End, draft);

    private static void Chain(IReadOnlyList<ScriptBlock> sequence, NodeId continuation, GraphDraft draft)
    {
        for (var position = 0; position < sequence.Count; position++)
        {
            var source = draft.IdOf(sequence[position]);
            if (draft.Node(source).Out.LeavesUnconditionally())
            {
                continue;
            }

            var next = position + 1 < sequence.Count
                ? draft.IdOf(sequence[position + 1])
                : continuation;
            draft.AddEdge(source, new Succession(next));
        }
    }
}

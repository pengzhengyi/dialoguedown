using DialogueDown.Graph.Builder;

namespace DialogueDown.Graph.Passes;

/// <summary>
/// Wires the default flow: each block falls through to the next block in document order, and the
/// last block falls through to the End node. Runs after node creation, once every block has an id.
/// </summary>
internal sealed class SuccessionPass : GraphBuildPass
{
    protected override void ApplyCore(GraphDraft draft, GraphBuildContext context)
    {
        var ids = context.Blocks.Select(draft.IdOf).ToArray();
        var successors = ids.Skip(1).Append(draft.End);

        foreach (var (source, target) in ids.Zip(successors))
        {
            draft.AddEdge(source, new Succession(target));
        }
    }
}

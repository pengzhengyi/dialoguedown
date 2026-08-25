using DialogueDown.Graph.Builder;
using DialogueDown.Graph.Edges;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Semantics;

namespace DialogueDown.Graph.Passes;

/// <summary>
/// Lowers each block's jumps to divert edges: a jump to a scene diverts to the node reaching that
/// scene lands on, and a jump to the reserved terminator (<c>#END</c>) diverts to the End node. A
/// jump's own condition rides along as the divert's condition. Runs before succession, which then skips
/// a node that already leaves unconditionally.
/// </summary>
internal sealed class DivertPass : GraphBuildPass
{
    protected override void ApplyCore(GraphDraft draft, GraphBuildContext context)
    {
        foreach (var block in context.AllBlocks)
        {
            foreach (var jump in block.Jumps())
            {
                if (TargetOf(jump, draft, context) is { } target)
                {
                    draft.AddEdge(draft.IdOf(block), new DivertEdge(target, jump.Label, jump.Condition));
                }
            }
        }
    }

    // Cross-file resolution is a later component, so a file-scoped target has no node to point at.
    private static NodeId? TargetOf(Jump jump, GraphDraft draft, GraphBuildContext context) =>
        context.ResolveJump(jump) switch
        {
            TerminalJump => draft.End,
            SceneJump target => EntryOf(target.Scene, draft, context),

            // A jump analysis could not resolve points nowhere — it already reported the missing
            // scene, or a target outside this script — so it wires no edge and the node keeps
            // falling through.
            UnresolvedJump or FileScopedJump => null,
            var resolution => throw new NotSupportedException(
                $"The dialogue graph builder does not yet lower {resolution.GetType().Name} jumps."),
        };

    // A scene whose content is exhausted falls through, so an entryless scene reaches the End.
    private static NodeId EntryOf(Scene scene, GraphDraft draft, GraphBuildContext context) =>
        context.EntryBlockOf(scene) is { } block ? draft.IdOf(block) : draft.End;
}

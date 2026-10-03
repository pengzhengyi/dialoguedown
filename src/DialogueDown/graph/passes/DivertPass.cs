using DialogueDown.Graph.Builder;
using DialogueDown.Graph.Edges;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Semantics;

namespace DialogueDown.Graph.Passes;

/// <summary>
/// Lowers each block's jumps to divert edges: a jump to a scene diverts to the node where play
/// enters that scene, and a jump to the reserved terminator (<c>#END</c>) diverts to the End node.
/// A jump's own condition becomes the divert's condition. Runs before succession, which then
/// skips a node that already leaves unconditionally.
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

    private static NodeId? TargetOf(Jump jump, GraphDraft draft, GraphBuildContext context) =>
        context.ResolveJump(jump) switch
        {
            TerminalJump => draft.End,
            SceneJump target => EntryOf(target.Scene, draft, context),

            // A jump to a missing scene or to another file has no node to point at. Analysis
            // already reported it, so no edge is added and the node falls through.
            UnresolvedJump or FileScopedJump => null,
            var resolution => throw new NotSupportedException(
                $"The dialogue graph builder does not yet lower {resolution.GetType().Name} jumps."),
        };

    // A scene with no block anywhere after its heading leads to the End.
    private static NodeId EntryOf(Scene scene, GraphDraft draft, GraphBuildContext context) =>
        context.EntryBlockOf(scene) is { } block ? draft.IdOf(block) : draft.End;
}

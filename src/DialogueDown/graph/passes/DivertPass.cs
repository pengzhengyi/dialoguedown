using DialogueDown.Graph.Builder;
using DialogueDown.Graph.Edges;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Semantics;

namespace DialogueDown.Graph.Passes;

/// <summary>
/// Lowers each block's jumps to divert edges: a jump to a scene diverts to the node reaching that
/// scene lands on, and a jump to the reserved terminator (<c>#END</c>) diverts to the End node. A
/// jump's own condition rides along as the divert's guard. Runs before succession, which then skips
/// a node that already leaves unconditionally.
/// </summary>
internal sealed class DivertPass : GraphBuildPass
{
    protected override void ApplyCore(GraphDraft draft, GraphBuildContext context)
    {
        foreach (var block in context.AllBlocks)
        {
            foreach (var jump in Jumps(block))
            {
                if (TargetOf(jump, draft, context) is { } target)
                {
                    draft.AddEdge(draft.IdOf(block), new DivertEdge(target, jump.Condition));
                }
            }
        }
    }

    private static IEnumerable<Jump> Jumps(ScriptBlock block) => block switch
    {
        Line line => line.Speech.OfType<Jump>(),
        ControlLine control => control.Effects.OfType<Jump>(),
        _ => [],
    };

    // Cross-file resolution is a later component, so a file-scoped target has no node to point at.
    private static NodeId? TargetOf(Jump jump, GraphDraft draft, GraphBuildContext context) =>
        context.ResolveJump(jump) switch
        {
            TerminalJump => draft.End,
            SceneJump target => EntryOf(target.Scene, draft, context),

            // An unresolved jump points nowhere — analysis already reported the missing scene — so
            // it wires no edge and the node keeps falling through.
            UnresolvedJump => null,
            var resolution => throw new NotSupportedException(
                $"The dialogue graph builder does not yet lower {resolution.GetType().Name} jumps."),
        };

    // A scene whose content is exhausted falls through, so an entryless scene reaches the End.
    private static NodeId EntryOf(Scene scene, GraphDraft draft, GraphBuildContext context) =>
        context.EntryBlockOf(scene) is { } block ? draft.IdOf(block) : draft.End;
}

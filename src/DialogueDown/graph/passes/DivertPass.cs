using DialogueDown.Graph.Builder;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Semantics;

namespace DialogueDown.Graph.Passes;

/// <summary>
/// Lowers each block's jumps to divert edges. A jump to the reserved terminator (<c>#END</c>)
/// diverts to the End node. Runs before succession, which then skips a node that already leaves
/// unconditionally.
/// </summary>
internal sealed class DivertPass : GraphBuildPass
{
    protected override void ApplyCore(GraphDraft draft, GraphBuildContext context)
    {
        foreach (var block in context.Blocks)
        {
            foreach (var jump in Jumps(block))
            {
                if (context.ResolveJump(jump) is TerminalJump)
                {
                    draft.AddEdge(draft.IdOf(block), new Divert(draft.End));
                }
            }
        }
    }

    private static IEnumerable<Jump> Jumps(ScriptBlock block) => block switch
    {
        Line line => line.Speech.OfType<Jump>(),
        _ => [],
    };
}

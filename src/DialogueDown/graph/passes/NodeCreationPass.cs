using DialogueDown.Graph.Builder;
using DialogueDown.Script.Ast;

namespace DialogueDown.Graph.Passes;

/// <summary>
/// Creates one node draft per script block in document order, then adds the terminal End node.
/// A later pass wires the edges between them.
/// </summary>
internal sealed class NodeCreationPass : GraphBuildPass
{
    protected override void ApplyCore(GraphDraft draft, GraphBuildContext context)
    {
        foreach (var block in context.Blocks)
        {
            AddNode(draft, context, block);
        }

        draft.AddEnd();
    }

    private static void AddNode(GraphDraft draft, GraphBuildContext context, ScriptBlock block)
    {
        switch (block)
        {
            case Line line:
                var speaker = context.ResolveSpeaker(SpeakerOf(line));
                draft.AddBlock(line, id => new LineNodeDraft(id, speaker, line.Speech));
                break;
            default:
                throw new NotSupportedException(
                    $"The dialogue graph builder does not yet lower {block.GetType().Name} blocks.");
        }
    }

    private static Speaker SpeakerOf(Line line) =>
        line.Speaker ?? throw new InvalidOperationException(
            "Analysis fills a default speaker on every line, so a line's speaker is never null here.");
}

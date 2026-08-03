using DialogueDown.Script.Ast;
using DialogueDown.Script.Semantics;

namespace DialogueDown.Graph;

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
            AddNode(draft, context.Semantics.Speakers, block);
        }

        draft.AddEnd();
    }

    private static void AddNode(GraphDraft draft, SpeakerTable speakers, ScriptBlock block)
    {
        switch (block)
        {
            case Line line:
                var speaker = speakers.Resolve(SpeakerOf(line));
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

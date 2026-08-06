using DialogueDown.Graph.Builder;
using DialogueDown.Script.Ast;

namespace DialogueDown.Graph.Passes;

/// <summary>
/// Creates one node draft per script block in document order — including the blocks nested in a
/// choice option's body — then adds the terminal End node.
/// A later pass wires the edges between them.
/// </summary>
internal sealed class NodeCreationPass : GraphBuildPass
{
    protected override void ApplyCore(GraphDraft draft, GraphBuildContext context)
    {
        foreach (var block in context.AllBlocks)
        {
            AddNode(draft, context, block);
        }

        draft.AddEnd();
    }

    private static void AddNode(GraphDraft draft, GraphBuildContext context, ScriptBlock block)
    {
        AssertUnguarded(block);
        switch (block)
        {
            case Line line:
                var speaker = context.ResolveSpeaker(SpeakerOf(line));
                draft.AddBlock(line, id => new LineNodeDraft(id, speaker, line.Speech));
                break;
            case Choices choices:
                draft.AddBlock(choices, id => new ChoiceNodeDraft(id, choices.IsOrdered));
                break;
            case RandomChoices random:
                draft.AddBlock(random, id => new RandomChoiceNodeDraft(id));
                break;
            case ControlLine control:
                draft.AddBlock(control, id => new ControlNodeDraft(id, [.. control.Effects.OfType<GameCall>()]));
                break;
            case ControlBlock conditional:
                draft.AddBlock(conditional, id => new BranchNodeDraft(id));
                break;
            default:
                throw new NotSupportedException(
                    $"The dialogue graph builder does not yet lower {block.GetType().Name} blocks.");
        }
    }

    // A guard on the block itself needs an edge that skips the block when it reads false, which no
    // pass wires yet.
    private static void AssertUnguarded(ScriptBlock block)
    {
        if (block is IConditional { Condition: not null })
        {
            throw new NotSupportedException(
                $"The dialogue graph builder does not yet lower a guarded {block.GetType().Name}.");
        }
    }

    private static Speaker SpeakerOf(Line line) =>
        line.Speaker ?? throw new InvalidOperationException(
            "Analysis fills a default speaker on every line, so a line's speaker is never null here.");
}

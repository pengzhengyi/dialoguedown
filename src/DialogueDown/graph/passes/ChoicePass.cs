using DialogueDown.Graph.Builder;
using DialogueDown.Graph.Edges;
using DialogueDown.Script.Ast;

namespace DialogueDown.Graph.Passes;

/// <summary>
/// Fans a choice group out into its options: one option edge per arm, leading to the first node of
/// that arm's body. Runs after node creation, and before succession — which then leaves the choice
/// without a fall-through, since taking an option is how control leaves it.
/// </summary>
internal sealed class ChoicePass : GraphBuildPass
{
    protected override void ApplyCore(GraphDraft draft, GraphBuildContext context)
    {
        foreach (var (block, continuation) in
                 BlockSequence.AllContinuations(context.TopLevelBlocks, draft.End, draft))
        {
            if (block is ChoiceGroup group)
            {
                FanOut(group, continuation, draft);
            }
        }
    }

    private static void FanOut(ChoiceGroup group, NodeId continuation, GraphDraft draft)
    {
        AssertNoGuardedOption(group);
        var choice = draft.IdOf(group);
        foreach (var body in group.OptionBodies())
        {
            var target = EntryOf(body, continuation, draft);
            var option = new OptionEdge(target);
            draft.AddEdge(choice, option);
        }
    }

    // An option with no content of its own plays nothing, so picking it resumes right where the
    // choice itself would have continued.
    private static NodeId EntryOf(
        IReadOnlyList<ScriptBlock> body, NodeId continuation, GraphDraft draft) =>
        body.Count > 0 ? draft.IdOf(body[0]) : continuation;

    // A guard decides whether an option is offered at all, which the option edge cannot yet carry.
    private static void AssertNoGuardedOption(ChoiceGroup group)
    {
        if (group.HasGuardedOption())
        {
            throw new NotSupportedException(
                "The dialogue graph builder does not yet lower a guarded choice option.");
        }
    }
}

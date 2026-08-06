using DialogueDown.Graph.Builder;
using DialogueDown.Graph.Edges;
using DialogueDown.Script.Ast;

namespace DialogueDown.Graph.Passes;

/// <summary>
/// Fans a choice group out into its arms: one edge per arm, leading to the first node of that arm's
/// body and carrying the guard that decides whether the arm is offered. A player choice emits plain
/// option edges; a random choice emits weighted ones, since the engine resolves the pick from the
/// weight. Runs after node creation, and before succession — which then gives the choice a
/// fall-through only when every arm is guarded, since then none may be available.
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
        var choice = draft.IdOf(group);
        foreach (var edge in ArmEdges(group, continuation, draft))
        {
            draft.AddEdge(choice, edge);
        }
    }

    // The two group kinds lead to their arms differently, so each builds the edge that carries what
    // its runtime needs.
    private static IEnumerable<Edge> ArmEdges(
        ChoiceGroup group, NodeId continuation, GraphDraft draft) => group switch
    {
        Choices choices => choices.Options.Select(option => (Edge)new OptionEdge(
            BlockSequence.EntryOf(option.Body, continuation, draft), option.Condition)),
        RandomChoices random => random.Options.Select(option => (Edge)new RandomOptionEdge(
            BlockSequence.EntryOf(option.Body, continuation, draft),
            option.Weight,
            option.Condition)),
        _ => throw new NotSupportedException(
            $"The dialogue graph builder does not yet lower {group.GetType().Name} groups."),
    };
}

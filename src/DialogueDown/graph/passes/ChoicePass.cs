using DialogueDown.Graph.Builder;
using DialogueDown.Graph.Edges;
using DialogueDown.Script.Ast;

namespace DialogueDown.Graph.Passes;

/// <summary>
/// Fans a choice group out into its options: one edge per option, leading to the first node of
/// that option's body and carrying the condition that decides whether the option is offered. A
/// player choice adds option edges carrying the words its menu shows; a random choice adds
/// weighted edges the engine picks from, which are never shown. Runs after node creation and
/// before succession, which gives a player choice a fall-through only when every option is
/// conditional, since then none may be available, and a random choice a fall-through only when it
/// may have nothing to draw.
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
        foreach (var edge in OptionEdges(group, continuation, draft))
        {
            draft.AddEdge(choice, edge);
        }
    }

    private static IEnumerable<Edge> OptionEdges(
        ChoiceGroup group, NodeId continuation, GraphDraft draft) => group switch
        {
            Choices choices => choices.Options.Select(option => (Edge)new OptionEdge(
                BlockSequence.EntryOf(option.Body, continuation, draft),
                option.Label(),
                option.Condition)),
            RandomChoices random => random.Options.Select(option => (Edge)new RandomOptionEdge(
                BlockSequence.EntryOf(option.Body, continuation, draft),
                option.Weight,
                option.Condition)),
            _ => throw new NotSupportedException(
                $"The dialogue graph builder does not yet lower {group.GetType().Name} groups."),
        };
}

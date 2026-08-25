using DialogueDown.Graph.Builder;
using DialogueDown.Graph.Edges;
using DialogueDown.Script.Ast;

namespace DialogueDown.Graph.Passes;

/// <summary>
/// Fans a choice group out into its arms: one edge per arm, leading to the first node of that arm's
/// body and carrying the condition that decides whether the arm is offered. A player choice emits plain
/// option edges; a random choice emits weighted ones, since the engine resolves the pick from the
/// weight. Runs after node creation, and before succession — which then gives the choice a
/// fall-through only when every arm is conditional, since then none may be available.
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

    // An arm is named by its own first block. It is read here, where the body is still in hand: an
    // arm with no body leads straight to whatever follows the choice, so a name read back off the
    // target would be somebody else's words.
    //
    // What names it is what the writer wrote there. Usually that is the words the arm speaks; for
    // an arm that only jumps — "- => [Take the east road](#the-market)", the ordinary way to write
    // a branching menu — it is the jump's own text, which would otherwise reach the graph only on
    // the divert edge one hop away, leaving the menu blank. The jump keeps its label there too:
    // the two answer different questions, one naming what the player is offered and the other what
    // the route is called, and a single piece of writing here plays both parts.
    private static IReadOnlyList<InlineFragment> LabelOf(Choice option) =>
        option.Body.FirstOrDefault() switch
        {
            Line line when line.Spoken() is { Count: > 0 } spoken => spoken,
            { } block => block.Jumps().FirstOrDefault()?.Label ?? [],
            _ => [],
        };

    // The two group kinds lead to their arms differently, so each builds the edge that carries what
    // its runtime needs.
    private static IEnumerable<Edge> ArmEdges(
        ChoiceGroup group, NodeId continuation, GraphDraft draft) => group switch
        {
            Choices choices => choices.Options.Select(option => (Edge)new OptionEdge(
                BlockSequence.EntryOf(option.Body, continuation, draft),
                LabelOf(option),
                option.Condition)),
            RandomChoices random => random.Options.Select(option => (Edge)new RandomOptionEdge(
                BlockSequence.EntryOf(option.Body, continuation, draft),
                option.Weight,
                option.Condition)),
            _ => throw new NotSupportedException(
                $"The dialogue graph builder does not yet lower {group.GetType().Name} groups."),
        };
}

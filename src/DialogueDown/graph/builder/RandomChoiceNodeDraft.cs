using DialogueDown.Common;
using DialogueDown.Graph.Edges;
using DialogueDown.Graph.Nodes;
using DialogueDown.Script.Ast;

namespace DialogueDown.Graph.Builder;

/// <summary>
/// A random choice under construction; a graph pass adds its random option edges.
/// </summary>
internal sealed class RandomChoiceNodeDraft(NodeId id, SourceSpan span) : NodeDraft(id, span)
{
    /// <inheritdoc/>
    /// <remarks>
    /// The engine leaves a random choice by the random option it draws, and it can draw only a
    /// random option whose share is above zero. An unguarded random option with a number above
    /// zero or an auto weight makes sure something is always drawable: the number is its own
    /// share, and beside an auto weight the shares in play add up to at least 100. Any other random
    /// option can have no share when the game runs: its guard can read false, the game can answer 0
    /// for its key weight, or its weight is <c>`0%`</c>. A random choice of only those needs a
    /// fall-through, as this one does for when the game answers 0 for both:
    /// <code>
    /// - `Bob.Affection%` Bob: Good to see you.
    /// - `Christina.Affection%` Christina: Oh, hello.
    /// </code>
    /// </remarks>
    public override bool LeavesUnconditionally() =>
        Out.OfType<RandomOptionEdge>().Any(GuaranteesSomethingDrawable);

    protected override DialogueNode CreateNode() =>
        new RandomChoiceNode(Id, Span, Out.ToArray());

    private static bool GuaranteesSomethingDrawable(RandomOptionEdge randomOption) =>
        randomOption.Condition is null
            && randomOption.Weight is AutoWeight or NumberWeight { Percentage: > 0 };
}

using System.Collections.Immutable;
using DialogueDown.Playbook.Edges;
using GraphEdges = DialogueDown.Graph.Edges;

namespace DialogueDown.Emission;

/// <summary>
/// Writes the ways out of a node.
/// </summary>
/// <remarks>
/// Every edge names its target by position, which is the one thing the graph cannot say for
/// itself: its node ids are opaque handles, so a numbering translates them here.
/// </remarks>
internal static class EdgeMapping
{
    /// <summary>Writes every way out of a node, in order.</summary>
    /// <param name="edges">The edges leaving the node.</param>
    /// <param name="numbering">Where each node will sit.</param>
    /// <returns>The same edges as a playbook carries them.</returns>
    public static ImmutableArray<Edge> Write(
        IReadOnlyList<GraphEdges.Edge> edges, NodeNumbering numbering)
    {
        ArgumentNullException.ThrowIfNull(edges);

        return [.. edges.Select(edge => Write(edge, numbering))];
    }

    /// <summary>Writes one way out.</summary>
    /// <param name="edge">The edge to write.</param>
    /// <param name="numbering">Where each node will sit.</param>
    /// <returns>The same edge as a playbook carries it.</returns>
    public static Edge Write(GraphEdges.Edge edge, NodeNumbering numbering)
    {
        ArgumentNullException.ThrowIfNull(edge);
        ArgumentNullException.ThrowIfNull(numbering);

        var target = numbering.Position(edge.Target);

        return edge switch
        {
            GraphEdges.SuccessionEdge => new SuccessionEdge(target),
            GraphEdges.OptionEdge option => new OptionEdge(
                target,
                SpeechMapping.Write(option.Label),
                ConditionMapping.Write(option.Condition)),
            GraphEdges.RandomOptionEdge random => new RandomOptionEdge(
                target,
                WeightMapping.Write(random.Weight),
                ConditionMapping.Write(random.Condition)),

            // The order arms are tried is otherwise lost: a reader is not obliged to keep a JSON
            // array in the order it was written, and an if/else that reorders tells another story.
            GraphEdges.BranchEdge branch => new BranchEdge(
                target, branch.Order, ConditionMapping.Write(branch.Condition)),

            GraphEdges.DivertEdge divert => new DivertEdge(
                target,
                SpeechMapping.Write(divert.Label),
                ConditionMapping.Write(divert.Condition)),

            _ => throw new NotSupportedException(
                $"No playbook edge is defined for {edge.GetType().Name}."),
        };
    }
}

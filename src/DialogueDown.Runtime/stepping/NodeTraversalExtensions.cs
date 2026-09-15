using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;

namespace DialogueDown.Runtime.Stepping;

/// <summary>
/// Which way out of a node a run takes.
/// </summary>
/// <remarks>
/// One reader per edge kind, the second axis a runner grows along: a divert, an option, and a
/// branch each pick their way out differently, and each arrives with the construct that needs it.
/// </remarks>
internal static class NodeTraversalExtensions
{
    /// <summary>Where a node leads when a run simply carries on.</summary>
    /// <remarks>
    /// A node falls through at most one way, so it carries one succession edge or none.
    /// </remarks>
    /// <param name="node">The node being left.</param>
    /// <returns>The node to arrive at, or <see langword="null"/> when nothing leads onward.</returns>
    public static int? SuccessionTarget(this Node node)
    {
        ArgumentNullException.ThrowIfNull(node);

        return node.Out.OfType<SuccessionEdge>().SingleOrDefault()?.Target;
    }
}

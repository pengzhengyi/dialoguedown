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

    /// <summary>Where a node leads on to.</summary>
    /// <remarks>
    /// A node may carry a jump and a fall-through both, and they are not alternatives at the same
    /// level: the jump is the way out the writer asked for, and the succession is where the run
    /// lands when there is no jump to take. So a node carrying a jump to 9 beside a succession to
    /// 4 leads to 9, and the 4 is unreachable.
    /// <para>
    /// Every jump that gets this far is one that always fires. A jump can also be written to fire
    /// only when the world allows it, and answering that needs a world this pass does not have —
    /// so a node carrying one is refused on arrival, and none reaches this read.
    /// </para>
    /// </remarks>
    /// <param name="node">The node being left.</param>
    /// <returns>The node to arrive at, or <see langword="null"/> when nothing leads onward.</returns>
    public static int? OnwardTarget(this Node node)
    {
        ArgumentNullException.ThrowIfNull(node);

        return node.Out.OfType<DivertEdge>().SingleOrDefault()?.Target ?? node.SuccessionTarget();
    }
}

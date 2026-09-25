using DialogueDown.Playbook.Conditions;
using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Stepping;

/// <summary>
/// Which way out of a node a run takes.
/// </summary>
/// <remarks>
/// The second axis a runner grows along: each kind of way out is read here once the runner plays
/// it. A jump and a block condition's arms are taken as the world allows, the first allowed in the
/// order written.
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

    /// <summary>Where a node leads on to, when no way out needs the world's answer.</summary>
    /// <remarks>
    /// A jump and a block condition's arms are the ways out a writer chose, and they come before
    /// the succession, which is where the run lands when none of them is taken. So a node carrying
    /// a jump to 9 beside a succession to 4 leads to 9, and the 4 is unreachable.
    /// <para>
    /// Without answers, only a way out that nothing guards is taken. A node with a guarded way out
    /// is asked about before it is left, and read on with the answers in hand.
    /// </para>
    /// </remarks>
    /// <param name="node">The node being left.</param>
    /// <returns>The node to arrive at, or <see langword="null"/> when nothing leads onward.</returns>
    public static int? OnwardTarget(this Node node)
    {
        ArgumentNullException.ThrowIfNull(node);

        return node.FirstWayOutTaken(way => way.Condition is null) ?? node.SuccessionTarget();
    }

    /// <summary>Where a node leads on to, once the world has answered its ways out.</summary>
    /// <remarks>
    /// The first way out the world allows is taken. A block condition's arms are tried in the order
    /// written, and its else is always allowed. A way out the world withholds is not a way out at
    /// all, so when none is allowed the run falls through to the succession written beneath them.
    /// That is how a withheld jump, or a block with no arm taken and no else, is skipped.
    /// </remarks>
    /// <param name="node">The node being left.</param>
    /// <param name="supply">What the world said about the keys its ways out asked about.</param>
    /// <returns>The node to arrive at, or <see langword="null"/> when nothing leads onward.</returns>
    public static int? OnwardTarget(this Node node, Supply supply)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(supply);

        return node.FirstWayOutTaken(way => way.IsAllowed(supply)) ?? node.SuccessionTarget();
    }

    // A reader has already put a block's arms in the order they are tried, so the first taken in
    // the order written is the first taken in the order tried.
    private static int? FirstWayOutTaken(this Node node, Func<IConditional, bool> isTaken) =>
        node.Out
            .FirstOrDefault(way => way is DivertEdge or BranchEdge && isTaken((IConditional)way))
            ?.Target;
}

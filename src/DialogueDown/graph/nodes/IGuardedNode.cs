using DialogueDown.Script.Ast;

namespace DialogueDown.Graph.Nodes;

/// <summary>
/// A node whose content plays only when its <see cref="Guard"/> holds. A guard means the same
/// thing here as on an <see cref="Edges.IGuardedEdge"/> — this may not happen — but withholds a
/// different thing: a guarded edge withholds a route, while a guarded node withholds its content.
/// The guard gates the whole block, so a node that reads false plays nothing and takes none of the
/// routes its content would have — a divert it holds is skipped along with the speech. A guarded
/// node therefore always has a succession edge to continue on, even when it also diverts, which is
/// the route control takes when the guard fails.
/// </summary>
internal interface IGuardedNode
{
    /// <summary>The condition the node's content plays under, or null when it always plays.</summary>
    Condition? Guard { get; }
}

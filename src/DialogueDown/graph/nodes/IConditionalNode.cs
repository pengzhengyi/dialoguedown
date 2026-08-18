using DialogueDown.Script.Ast;

namespace DialogueDown.Graph.Nodes;

/// <summary>
/// A node whose content plays only when its <see cref="Condition"/> holds. A condition means the
/// same thing here as on an <see cref="Edges.IConditionalEdge"/> — this may not happen — but
/// withholds a different thing: a conditional edge withholds a route, while a conditional node
/// withholds its content. The condition covers the whole block, so a node that reads false plays
/// nothing and takes none of the routes its content would have — a divert it holds is skipped
/// along with the speech. A conditional node therefore always has a succession edge to continue
/// on, even when it also diverts, which is the route control takes when the condition fails.
/// </summary>
internal interface IConditionalNode
{
    /// <summary>The condition the node's content plays under, or null when it always plays.</summary>
    Condition? Condition { get; }
}

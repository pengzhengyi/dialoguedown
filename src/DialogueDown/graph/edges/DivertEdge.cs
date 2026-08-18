using DialogueDown.Script.Ast;

namespace DialogueDown.Graph.Edges;

/// <summary>
/// A non-returning jump edge: control transfers to the <see cref="Edge.Target"/> and does not
/// return. An unconditional divert is taken unconditionally, so its source node does not fall through;
/// a conditional divert fires only when its condition reads true.
/// </summary>
internal sealed record DivertEdge(NodeId Target, Condition? Condition = null)
    : Edge(Target), IConditionalEdge;

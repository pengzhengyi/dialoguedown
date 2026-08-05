using DialogueDown.Script.Ast;

namespace DialogueDown.Graph.Edges;

/// <summary>
/// A non-returning jump edge: control transfers to the <see cref="Edge.Target"/> and does not
/// return. An unguarded divert (a null <see cref="Guard"/>) is taken unconditionally, so its source
/// node does not fall through; a guarded divert fires only when its guard reads true.
/// </summary>
internal sealed record DivertEdge(NodeId Target, Condition? Guard = null) : Edge(Target);

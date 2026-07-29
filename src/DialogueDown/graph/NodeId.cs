namespace DialogueDown.Graph;

/// <summary>
/// A node's stable identity in a <see cref="DialogueGraph"/>. Edges reference their target by
/// id rather than by object, so a cyclic graph needs no mutable back-references and the graph
/// stays immutable. The value is an <b>opaque handle</b>: the builder assigns sequential ids
/// today, but callers resolve a node through <see cref="DialogueGraph.Node"/> and never treat
/// the value as a list index — so a future incremental or just-in-time build can assign
/// source-derived, stable ids without changing any caller.
/// </summary>
internal readonly record struct NodeId(int Value);

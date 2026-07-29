namespace DialogueDown.Graph;

/// <summary>
/// A node's identity in a <see cref="DialogueGraph"/>. Edges reference their target by id rather
/// than by object, so a cyclic graph needs no back-references. It is an opaque handle: callers
/// resolve a node through <see cref="DialogueGraph.Node"/> rather than treating the value as a
/// list index.
/// </summary>
internal readonly record struct NodeId(int Value);

namespace DialogueDown.Graph;

/// <summary>
/// A directed connection from one node to a <see cref="Target"/>, of a specific kind. An edge
/// names its target by <see cref="NodeId"/>, so an edge to an earlier node is an ordinary cycle.
/// </summary>
internal abstract record Edge(NodeId Target);

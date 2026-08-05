namespace DialogueDown.Graph.Edges;

/// <summary>
/// The default edge: control falls through to the next block in document order.
/// </summary>
internal sealed record SuccessionEdge(NodeId Target) : Edge(Target);

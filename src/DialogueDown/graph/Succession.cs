namespace DialogueDown.Graph;

/// <summary>
/// The default edge: control falls through to the next block in document order.
/// </summary>
internal sealed record Succession(NodeId Target) : Edge(Target);

namespace DialogueDown.Graph;

/// <summary>
/// The terminal node of a run: reaching it ends the dialogue. The reserved <c>#END</c> target
/// and running off the end of the document both lead here. It has no outgoing edges.
/// </summary>
internal sealed record EndNode(NodeId Id) : DialogueNode(Id);

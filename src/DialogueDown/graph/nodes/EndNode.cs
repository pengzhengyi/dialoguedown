using DialogueDown.Common;

namespace DialogueDown.Graph.Nodes;

/// <summary>
/// The terminal node: reaching it ends the dialogue. The reserved <c>#END</c> target and running
/// off the end of the document both lead here. It has no outgoing edges. Being synthetic it owns
/// no source text, so its <see cref="Span"/> is zero-width, just past the document's last block.
/// </summary>
internal sealed record EndNode(NodeId Id, SourceSpan Span) : DialogueNode(Id, Span, []);

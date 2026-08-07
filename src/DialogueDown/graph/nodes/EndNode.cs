using DialogueDown.Common;

namespace DialogueDown.Graph.Nodes;

/// <summary>
/// The terminal node of a run: reaching it ends the dialogue. The reserved <c>#END</c> target
/// and running off the end of the document both lead here. It has no outgoing edges. Being
/// synthetic it owns no source text, so its <see cref="Span"/> is zero-width where the document
/// ends — the caret position a tool marks the end of a run at.
/// </summary>
internal sealed record EndNode(NodeId Id, SourceSpan Span) : DialogueNode(Id, Span, []);

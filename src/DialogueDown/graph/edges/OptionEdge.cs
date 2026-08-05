namespace DialogueDown.Graph.Edges;

/// <summary>
/// One arm of a <see cref="Nodes.ChoiceNode"/>: the body it leads to. The option's displayed text
/// is the speech of its <see cref="Edge.Target"/> — the body's first node.
/// </summary>
internal sealed record OptionEdge(NodeId Target) : Edge(Target);

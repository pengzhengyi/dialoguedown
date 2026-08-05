namespace DialogueDown.Graph;

/// <summary>
/// One arm of a <see cref="ChoiceNode"/>: the body it leads to. The option's displayed text is
/// the speech of its <see cref="Edge.Target"/> — the body's first node.
/// </summary>
internal sealed record Option(NodeId Target) : Edge(Target);

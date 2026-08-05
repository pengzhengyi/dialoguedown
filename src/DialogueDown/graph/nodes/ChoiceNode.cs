namespace DialogueDown.Graph.Nodes;

/// <summary>
/// A branch point: control leaves it by taking exactly one of its <see cref="Option"/> edges,
/// so it never falls through. It holds no content of its own — an option's text is the first
/// node of the body it leads to.
/// </summary>
internal sealed record ChoiceNode(NodeId Id, IReadOnlyList<Edge> Out) : DialogueNode(Id, Out);

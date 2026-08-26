namespace DialogueDown.Runtime.Positions;

/// <summary>
/// Standing at a node, ready to be advanced.
/// </summary>
/// <param name="Node">The node's position in the playbook.</param>
public sealed record AtNode(int Node) : Position;

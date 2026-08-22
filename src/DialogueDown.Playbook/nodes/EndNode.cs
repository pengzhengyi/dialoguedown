namespace DialogueDown.Playbook.Nodes;

/// <summary>
/// Where a run stops. It leads nowhere, so it has no ways out.
/// </summary>
/// <param name="Id">This node's position in the node list.</param>
public sealed record EndNode(int Id) : Node(Id, []);

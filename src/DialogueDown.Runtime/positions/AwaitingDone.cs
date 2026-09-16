namespace DialogueDown.Runtime.Positions;

/// <summary>
/// Standing at a node, waiting for the host to say what it asked for was carried out.
/// </summary>
/// <remarks>
/// The same node a run would otherwise stand at, at a different stage: it has asked the host to
/// change the world and cannot read on until that change is made, because what it reads next may
/// depend on it. Carrying the stage here rather than beside the position is what keeps the two
/// from disagreeing, and it is what lets the protocol say which command belongs where without
/// going back to the playbook to work it out.
/// </remarks>
/// <param name="Node">The node's position in the playbook.</param>
public sealed record AwaitingDone(int Node) : Position;

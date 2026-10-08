namespace DialogueDown.Runtime.Situations;

/// <summary>
/// Standing at a node, waiting for the host to say what it asked for was carried out.
/// </summary>
/// <remarks>
/// The run has sent <c>Perform</c> and waits for <c>Done</c> before going on, because what comes
/// next may depend on the change: a condition after an effect must see the world the effect left
/// behind.
/// </remarks>
/// <param name="Node">The node's position in the playbook.</param>
/// <param name="Resume">Where the node carries on once the host is done.</param>
public sealed record AwaitingDone(int Node, Resume Resume) : Situation;

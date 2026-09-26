namespace DialogueDown.Runtime.Situations;

/// <summary>
/// Standing at a node, waiting for the host to say what it asked for was carried out.
/// </summary>
/// <remarks>
/// The run has reached the node and asked the host to change the world. It cannot read on until
/// that change is made, because what it reads next may depend on it — a guard that follows an
/// effect must see the world the effect left behind.
/// <para>
/// Saying so here, rather than in a flag beside the node, is what lets the protocol tell which
/// command belongs where without going back to the playbook to work it out.
/// </para>
/// </remarks>
/// <param name="Node">The node's position in the playbook.</param>
public sealed record AwaitingDone(int Node) : Situation;

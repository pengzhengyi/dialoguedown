namespace DialogueDown.Runtime.Situations;

/// <summary>
/// Standing at a node, ready to be advanced.
/// </summary>
/// <remarks>
/// The node has been played and nothing is waiting on the host; <c>Next</c> moves the run on. A
/// line that has been said leaves a run here.
/// </remarks>
/// <param name="Node">The node's position in the playbook.</param>
public sealed record AtNode(int Node) : Situation;

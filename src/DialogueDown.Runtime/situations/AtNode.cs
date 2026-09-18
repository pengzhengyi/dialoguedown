namespace DialogueDown.Runtime.Situations;

/// <summary>
/// Standing at a node, ready to be advanced.
/// </summary>
/// <remarks>
/// The plainest of them: the node has been played, nobody is waiting on anything, and
/// <c>Next</c> carries the run on. A line that has been said leaves a run here.
/// </remarks>
/// <param name="Node">The node's position in the playbook.</param>
public sealed record AtNode(int Node) : Situation;

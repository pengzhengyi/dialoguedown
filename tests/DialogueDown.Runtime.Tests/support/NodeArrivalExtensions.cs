using DialogueDown.Playbook.Nodes;
using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Stepping;
using static DialogueDown.Runtime.Tests.PlaybookNodes;

namespace DialogueDown.Runtime.Tests;

/// <summary>
/// What the runner does when it arrives at a single node.
/// </summary>
/// <remarks>
/// Each helper places the node in the smallest playbook that can hold it: the node, then an end
/// node.
/// </remarks>
internal static class NodeArrivalExtensions
{
    /// <summary>How the runner refuses a node of this kind, if it refuses one.</summary>
    /// <param name="node">The node to arrive at.</param>
    /// <returns>The refusal, or <see langword="null"/> when this build plays such a node.</returns>
    public static Refused? RefusalOnArrival(this Node node) =>
        Arrival.At(PlayContextFactory.Of([node, End(1)], ["Alice"]), 0)
            .Events.OfType<Refused>()
            .FirstOrDefault();

    /// <summary>Whether the runner has no code for a node of this kind yet.</summary>
    /// <remarks>
    /// Narrower than refusing: a node can also be refused for the data it holds, such as a key it
    /// needs both as a truth and as words.
    /// </remarks>
    /// <param name="node">The node to arrive at.</param>
    /// <returns>
    /// <see langword="true"/> when arriving refuses because the runner has no code for the kind.
    /// </returns>
    public static bool IsUnplayableKind(this Node node) =>
        node.RefusalOnArrival()?.Reason == RefusalReason.UnplayableNode;
}

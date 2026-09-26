using DialogueDown.Playbook.Nodes;
using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Stepping;
using static DialogueDown.Runtime.Tests.PlaybookNodes;

namespace DialogueDown.Runtime.Tests;

/// <summary>
/// What the runner does when it arrives at a single node.
/// </summary>
/// <remarks>
/// Several tests ask what the runner does with a node of a given kind, and none of them cares
/// about the rest of the playbook. So this helper builds the smallest playbook that can hold one:
/// the node, followed by an end node. The tests then ask their question without repeating that
/// setup.
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
    /// Narrower than refusing. A node can also be refused for the data it holds, such as one key
    /// it needs as a truth and as words both. Only an untaught kind means nobody has written the
    /// code that plays it.
    /// </remarks>
    /// <param name="node">The node to arrive at.</param>
    /// <returns><see langword="true"/> when arriving refuses because the kind is untaught.</returns>
    public static bool IsUntaught(this Node node) =>
        node.RefusalOnArrival()?.Reason == RefusalReason.UnplayableNode;
}

using DialogueDown.Playbook.Nodes;
using DialogueDown.Runtime.Stepping;
using static DialogueDown.TestSupport.PlaybookNodeFactory;

namespace DialogueDown.Runtime.Tests;

/// <summary>
/// Whether the runner can play a node of a given kind.
/// </summary>
/// <remarks>
/// The node is placed in the smallest playbook that can hold it: the node, then an end node.
/// </remarks>
internal static class NodePlayingExtensions
{
    /// <summary>Whether the runner has no playing for a node of this kind.</summary>
    /// <param name="node">The node to arrive at.</param>
    /// <returns>
    /// <see langword="true"/> when arriving at the node throws <see cref="NotSupportedException"/>,
    /// as the runner does at a kind it has no playing for.
    /// </returns>
    /// <remarks>
    /// Any other exception is let through, since it is a fault this check does not ask about.
    /// </remarks>
    public static bool IsUnplayableKind(this Node node)
    {
        try
        {
            _ = Arrival.At(PlayContextFactory.Of([node, End(id: 1)], ["Alice"]), 0);
        }
        catch (NotSupportedException)
        {
            return true;
        }

        return false;
    }
}

using System.Globalization;

namespace DialogueDown.Playbook.Checking;

/// <summary>
/// Refuses a playbook whose nodes are not where they say they are.
/// </summary>
/// <remarks>
/// A node's id must equal its index in <see cref="PlaybookDocument.Nodes"/>, since edges, anchors,
/// and the entry name a node by that index. Comparing each id with its index proves the ids are
/// unique, gapless, and in order. Without it, a reordered node list (from a merge or a tool) would
/// send every edge to a different node with no error.
/// </remarks>
public sealed class NodePositionChecker : IPlaybookChecker
{
    /// <inheritdoc/>
    public void Check(PlaybookDocument playbook)
    {
        ArgumentNullException.ThrowIfNull(playbook);

        for (var index = 0; index < playbook.Nodes.Length; index++)
        {
            var claimed = playbook.Nodes[index].Id;

            if (claimed != index)
            {
                throw new InvalidPlaybookException(
                    $"Node at index {Position(index)} claims id {Position(claimed)}; " +
                    "a node's id is its position.");
            }
        }
    }

    private static string Position(int index) => index.ToString(CultureInfo.InvariantCulture);
}

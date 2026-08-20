using System.Globalization;

namespace DialogueDown.Playbook;

/// <summary>
/// Refuses a playbook whose nodes are not where they say they are.
/// </summary>
/// <remarks>
/// <para>
/// A node's id is its position. The compiler's own node identity is an opaque handle, so writing
/// a playbook renumbers every node into a dense index — which turns an identity you can only look
/// up into an address you can check.
/// </para>
/// <para>
/// This is that check, and it is the one every other index in the document rests on. One
/// comparison per node proves the ids are unique, gapless, and in order all at once. Skip it and
/// a reordered node list — from a merge, a formatter, a well-meaning tool — silently redirects
/// every edge that names a node: the story still plays, just not the one that was written.
/// </para>
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

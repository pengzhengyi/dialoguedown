using System.Globalization;

namespace DialogueDown.Playbook;

/// <summary>
/// Refuses a playbook that points somewhere it does not have.
/// </summary>
/// <remarks>
/// <para>
/// Every address in a playbook is a plain index — into the nodes, or into the speakers — so a
/// reference is sound exactly when it lands. These are the rules a schema cannot state, because
/// each relates a value to something else in the same document.
/// </para>
/// <para>
/// Only the upper bound is checked here. A negative index is refused when the value is built, so
/// by the time a document exists there is no such thing.
/// </para>
/// </remarks>
public sealed class ReferenceChecker : IPlaybookChecker
{
    /// <inheritdoc/>
    public void Check(PlaybookDocument playbook)
    {
        ArgumentNullException.ThrowIfNull(playbook);

        var nodes = playbook.Nodes.Length;
        var speakers = playbook.Speakers.Length;

        AssertLands(playbook.Entry, nodes, "The entry");

        for (var index = 0; index < nodes; index++)
        {
            var node = playbook.Nodes[index];

            if (node is LineNode line)
            {
                AssertLands(line.Speaker, speakers, $"The speaker of node {Position(index)}");
            }

            foreach (var edge in node.Out)
            {
                AssertLands(edge.Target, nodes, $"An edge out of node {Position(index)}");
            }
        }

        foreach (var anchor in playbook.Anchors)
        {
            AssertLands(anchor.Value, nodes, $"The anchor '{anchor.Key}'");
        }
    }

    private static void AssertLands(int reference, int count, string what)
    {
        if (reference >= count)
        {
            throw new InvalidPlaybookException(
                $"{what} points at {Position(reference)}, but there are only {Position(count)}.");
        }
    }

    private static string Position(int index) => index.ToString(CultureInfo.InvariantCulture);
}

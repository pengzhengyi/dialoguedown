using System.Collections.Immutable;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Speakers;
using DialogueDown.Visualization.Playbook;

namespace DialogueDown.Visualization.Tests.Support;

/// <summary>
/// Checks the line a node's row in the playbook table shows.
/// </summary>
/// <remarks>
/// The summary travels as labeled pieces; the line a reader sees is those pieces joined. A test
/// about the pieces themselves calls <see cref="PlaybookNodeSummary.SegmentsOf"/> directly.
/// </remarks>
internal static class PlaybookNodeSummaryAssert
{
    /// <summary>Asserts the line a node's row shows, for a node that names no speaker.</summary>
    public static void AssertSummary(string expected, Node node) =>
        AssertSummary(expected, node, []);

    /// <summary>Asserts the line a node's row shows, reading speakers from the list given.</summary>
    public static void AssertSummary(
        string expected, Node node, ImmutableArray<PlaybookSpeaker> speakers) =>
        Assert.Equal(expected, SummaryOf(node, speakers));

    /// <summary>The text a node's pieces join into: the line its table cell shows.</summary>
    public static string SummaryOf(Node node, ImmutableArray<PlaybookSpeaker> speakers) =>
        string.Concat(PlaybookNodeSummary.SegmentsOf(node, speakers).Select(segment => segment.Text));
}

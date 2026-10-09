using System.Collections.Immutable;
using System.Text.Json;
using DialogueDown.Compilation;
using DialogueDown.Emission;
using DialogueDown.Playbook;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Speakers;

using DialogueDown.Visualization.Display;

namespace DialogueDown.Visualization.Playbook;

/// <summary>
/// Projects a compile into the report's <see cref="PlaybookReport"/>: the playbook a runtime
/// would load, beside the header facts and speakers a reader would otherwise scroll to find.
/// </summary>
/// <remarks>
/// Only a clean compile has a graph, and only a graph becomes a playbook, so a script with errors
/// projects to an explanation rather than an empty document. That mirrors the Dialogue Graph
/// stage, which is unavailable on the same condition and for the same reason.
/// </remarks>
internal static class PlaybookProjection
{
    /// <summary>Shown instead of a playbook when the compile did not produce one.</summary>
    internal const string UnavailableReason =
        "A playbook is written only for a script that compiles without errors.";

    // The categories are the Dialogue Graph's, so a kind keeps one color across both tabs.
    private const string SpeechCategory = "speech";
    private const string CallCategory = "call";
    private const string StructureCategory = "structure";
    private const string TerminalCategory = "terminal";

    /// <summary>
    /// Projects the playbook a compile produced.
    /// </summary>
    /// <param name="result">The compile to project.</param>
    /// <param name="script">The script name the playbook should report.</param>
    /// <param name="writer">The writer that turns a compile into a playbook.</param>
    /// <returns>The playbook section of the report payload.</returns>
    public static PlaybookReport Project(
        CompilationResult result, string script, IPlaybookWriter writer)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(writer);

        if (result is not CompilationSuccess success)
        {
            return new PlaybookReport(null, null, [], [], [], UnavailableReason);
        }

        var playbook = writer.Write(success, script);
        return new PlaybookReport(
            // The same options `ddown compile --emit playbook` writes with, so the tab shows that text.
            JsonSerializer.Serialize(playbook, PlaybookJson.Options),
            MetadataOf(playbook, script),
            [.. playbook.Speakers.Select(ToView)],
            [.. playbook.Anchors.Select(anchor => new PlaybookAnchorView(anchor.Key, anchor.Value))],
            [.. playbook.Nodes.Select(node => ToView(node, playbook.Speakers))],
            null);
    }

    private static PlaybookMetadataView MetadataOf(PlaybookDocument playbook, string script) =>
        new(
            script,
            playbook.Format.Version,
            PlaybookWriter.SchemaUrl,
            [.. playbook.Format.Requires],
            [.. playbook.Format.Uses],
            playbook.Entry,
            playbook.Nodes.Length,
            playbook.Anchors.Count);

    private static PlaybookSpeakerView ToView(PlaybookSpeaker speaker) =>
        new(
            speaker.Id,
            speaker.Name,
            speaker.Default,
            [.. speaker.Tags.Select(tag => new TagView(tag.Name, tag.Value, tag.Reserved))]);

    private static PlaybookNodeView ToView(
        Node node, ImmutableArray<PlaybookSpeaker> speakers)
    {
        var (kind, category) = node switch
        {
            LineNode => (NodeKinds.Line, SpeechCategory),
            ChoiceNode => (NodeKinds.Choice, StructureCategory),
            RandomChoiceNode => (NodeKinds.RandomChoice, StructureCategory),
            BranchNode => (NodeKinds.Branch, StructureCategory),
            ControlNode => (NodeKinds.Control, CallCategory),
            EndNode => (NodeKinds.End, TerminalCategory),
            // A kind nobody has accounted for still gets a row, named by its type and grouped
            // with the structural nodes, which is what the Dialogue Graph does with one too.
            _ => (node.GetType().Name, StructureCategory),
        };

        return new PlaybookNodeView(
            node.Id,
            kind,
            category,
            PlaybookNodeSummary.SegmentsOf(node, speakers),
            [.. node.Out.Select(edge => edge.Target)]);
    }
}

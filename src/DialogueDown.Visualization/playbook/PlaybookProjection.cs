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

    // The report shows the playbook to be read, not to be diffed byte-for-byte against a file, so
    // it is indented here. `ddown compile --emit playbook` writes the same document compactly.
    private static readonly JsonSerializerOptions _readable = Readable();

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
            JsonSerializer.Serialize(playbook, _readable),
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

    // A tag reads as `name=value` when it carries one, so the table shows what the script wrote
    // rather than a name whose value is invisible.
    private static PlaybookSpeakerView ToView(PlaybookSpeaker speaker) =>
        new(
            speaker.Id,
            speaker.Name,
            speaker.Default,
            [.. speaker.Tags.Select(tag => new TagView(tag.Name, tag.Value, tag.Reserved))]);

    // The wire tag and the color category are read together so the correspondence between them
    // stays in one place rather than spread across two matches over the same types.
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

        var segments = PlaybookNodeSummary.SegmentsOf(node, speakers);

        return new PlaybookNodeView(
            node.Id,
            kind,
            category,
            string.Concat(segments.Select(segment => segment.Text)),
            [.. segments],
            [.. node.Out.Select(edge => edge.Target)]);
    }

    private static JsonSerializerOptions Readable()
    {
        var options = new JsonSerializerOptions(PlaybookJson.Options) { WriteIndented = true };
        return options;
    }
}

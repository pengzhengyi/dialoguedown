using System.Text.Json;
using DialogueDown.Compilation;
using DialogueDown.Emission;
using DialogueDown.Playbook;
using DialogueDown.Playbook.Speakers;

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
            return new PlaybookReport(null, null, [], UnavailableReason);
        }

        var playbook = writer.Write(success, script);
        return new PlaybookReport(
            JsonSerializer.Serialize(playbook, _readable),
            MetadataOf(playbook, script),
            [.. playbook.Speakers.Select(ToView)],
            null);
    }

    private static PlaybookMetadataView MetadataOf(PlaybookDocument playbook, string script) =>
        new(
            script,
            playbook.Format.Version,
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
            [.. speaker.Tags.Select(tag => tag.Value is null ? tag.Name : $"{tag.Name}={tag.Value}")]);

    private static JsonSerializerOptions Readable()
    {
        var options = new JsonSerializerOptions(PlaybookJson.Options) { WriteIndented = true };
        return options;
    }
}

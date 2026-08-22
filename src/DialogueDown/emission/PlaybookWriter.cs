using System.Diagnostics.CodeAnalysis;
using DialogueDown.Compilation;
using DialogueDown.Graph;
using DialogueDown.Playbook;

namespace DialogueDown.Emission;

/// <summary>
/// Writes the playbook this build's format describes.
/// </summary>
/// <remarks>
/// Nothing here decides anything: the numberings answer where each node and speaker sits, and a
/// mapping per kind says what each becomes. This assembles their answers into one document.
/// </remarks>
internal sealed class PlaybookWriter : IPlaybookWriter
{
    /// <summary>The playbook format version this build writes.</summary>
    public const int FormatVersion = 0;

    /// <summary>Where the schema for the version this build writes is published.</summary>
    /// <remarks>
    /// Written into every playbook so an editor validates one wherever it lands, without the
    /// person who opened it having to know the format exists. Built from
    /// <see cref="FormatVersion"/> rather than written out, so the two cannot disagree.
    /// </remarks>
    [SuppressMessage(
        "Major Code Smell",
        "S1075:URIs should not be hardcoded",
        Justification = "The published identity of the format, not a setting.")]
    public static string SchemaUrl =>
        $"https://pengzhengyi.github.io/dialoguedown/schema/{SchemaFileName}";

    /// <summary>What the schema for that version is called in this repository.</summary>
    /// <remarks>
    /// Both are properties rather than fields: a field would have to be declared before the one
    /// that reads it, and nothing warns when reordering two static initializers leaves one of
    /// them reading an empty string.
    /// </remarks>
    public static string SchemaFileName => $"playbook-{FormatVersion}.schema.json";

    /// <inheritdoc/>
    public PlaybookDocument Write(CompilationSuccess compilation, string script)
    {
        ArgumentNullException.ThrowIfNull(compilation);

        return Write(compilation.Graph, script);
    }

    /// <summary>
    /// Writes a graph as a playbook.
    /// </summary>
    /// <param name="graph">The flow to write.</param>
    /// <param name="script">The script this was compiled from.</param>
    /// <returns>The playbook, ready to serialize.</returns>
    public PlaybookDocument Write(DialogueGraph graph, string script)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(script);

        var nodes = NodeNumbering.Of(graph.Nodes);
        var speakers = SpeakerNumbering.Of(graph.Nodes);

        return new PlaybookDocument(
            Format(),
            script,
            nodes.Position(graph.Entry),
            AnchorMapping.Write(graph.Regions, nodes),
            speakers.Speakers,
            [.. graph.Nodes.Select(node => NodeMapping.Write(node, nodes, speakers))],
            SchemaUrl);
    }

    // Every graph version 0 emits needs only the core constructs. When a construct that gates
    // playback arrives, this becomes a scan of what was written rather than a constant.
    private static PlaybookFormat Format() => new(FormatVersion, [Capabilities.Core], []);
}

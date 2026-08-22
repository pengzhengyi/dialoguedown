using System.Collections.Immutable;
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
            ImmutableSortedDictionary<string, int>.Empty,
            speakers.Speakers,
            [.. graph.Nodes.Select(node => NodeMapping.Write(node, nodes, speakers))]);
    }

    // Every graph version 0 emits needs only the core constructs. When a construct that gates
    // playback arrives, this becomes a scan of what was written rather than a constant.
    private static PlaybookFormat Format() => new(FormatVersion, [Capabilities.Core], []);
}

using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace DialogueDown.Playbook;

/// <summary>
/// One compiled script: everything a runtime needs to play it, and nothing else.
/// </summary>
/// <remarks>
/// Named a document rather than a playbook because a type may not share its namespace's name
/// without shadowing it.
/// </remarks>
public sealed record PlaybookDocument
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybookDocument"/> class.
    /// </summary>
    /// <param name="format">Whether a runtime can play this at all.</param>
    /// <param name="script">The script this was compiled from.</param>
    /// <param name="entry">Where a playthrough begins by default.</param>
    /// <param name="anchors">Every scene's slug, by node index.</param>
    /// <param name="speakers">Everybody who speaks here.</param>
    /// <param name="nodes">The steps of a playthrough, each at its own index.</param>
    /// <param name="schema">Where an editor can find the schema, if the writer said.</param>
    [JsonConstructor]
    public PlaybookDocument(
        PlaybookFormat format,
        string script,
        int entry,
        ImmutableSortedDictionary<string, int> anchors,
        ImmutableArray<PlaybookSpeaker> speakers,
        ImmutableArray<Node> nodes,
        string? schema = null)
    {
        ArgumentNullException.ThrowIfNull(format);

        Schema = schema;
        Format = format;
        Script = script.AssertNotNull(nameof(script));
        Entry = entry.AssertNotNegative(nameof(entry));
        Anchors = anchors ?? ImmutableSortedDictionary<string, int>.Empty;
        Speakers = speakers.OrEmpty();
        Nodes = nodes.OrEmpty();
    }

    /// <summary>Gets where an editor can find the schema, if the writer said.</summary>
    [JsonPropertyOrder(0)]
    [JsonPropertyName("$schema")]
    public string? Schema { get; }

    /// <summary>Gets whether a runtime can play this at all.</summary>
    [JsonPropertyOrder(1)]
    [JsonPropertyName("format")]
    public PlaybookFormat Format { get; }

    /// <summary>Gets the script this was compiled from.</summary>
    [JsonPropertyOrder(2)]
    [JsonPropertyName("script")]
    public string Script { get; }

    /// <summary>Gets where a playthrough begins when nothing says otherwise.</summary>
    /// <remarks>
    /// The document's top, which has no heading and so is not an anchor. Stated rather than
    /// assumed to be the first node, because that is the sort of assumption a later hoisted
    /// construct breaks quietly.
    /// </remarks>
    [JsonPropertyOrder(3)]
    [JsonPropertyName("entry")]
    public int Entry { get; }

    /// <summary>
    /// Gets every scene's slug, by node index — the targets a jump may name, and the named
    /// conversations a game may start at instead of the top.
    /// </summary>
    /// <remarks>
    /// Sorted, because a lookup table's order carries no meaning but a golden file needs one:
    /// sorting makes a playbook byte-identical however the writer happened to build it.
    /// </remarks>
    [JsonPropertyOrder(4)]
    [JsonPropertyName("anchors")]
    public ImmutableSortedDictionary<string, int> Anchors { get; }

    /// <summary>Gets everybody who speaks here.</summary>
    [JsonPropertyOrder(5)]
    [JsonPropertyName("speakers")]
    public ImmutableArray<PlaybookSpeaker> Speakers { get; }

    /// <summary>Gets the steps of a playthrough, each at its own index.</summary>
    [JsonPropertyOrder(6)]
    [JsonPropertyName("nodes")]
    public ImmutableArray<Node> Nodes { get; }
}

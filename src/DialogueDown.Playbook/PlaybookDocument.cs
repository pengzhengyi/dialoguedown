using System.Collections.Immutable;
using System.Text.Json.Serialization;
using DialogueDown.Playbook.Common;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Speakers;
using Generator.Equals;

namespace DialogueDown.Playbook;

/// <summary>
/// One compiled script: everything a runtime needs to play it, and nothing else.
/// </summary>
/// <remarks>
/// Named a document rather than a playbook because a type may not share its namespace's name
/// without shadowing it.
/// </remarks>
[Equatable]
public sealed partial record PlaybookDocument
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybookDocument"/> class.
    /// </summary>
    /// <param name="format">Whether a runtime can play this at all.</param>
    /// <param name="script">The script this was compiled from.</param>
    /// <param name="entry">Where a playthrough begins by default.</param>
    /// <param name="anchors">Each scene's slug, mapped to the index of the node it starts at.</param>
    /// <param name="speakers">Everybody who speaks here.</param>
    /// <param name="nodes">The steps of a playthrough, each at its own index.</param>
    /// <param name="schema">Where an editor can find the JSON schema, or <c>null</c> when none is named.</param>
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
        // Re-sorted ordinally whatever comparer the caller used; see Anchors.
        Anchors = (anchors ?? ImmutableSortedDictionary<string, int>.Empty)
            .WithComparers(StringComparer.Ordinal);
        Speakers = speakers.OrEmpty();
        Nodes = nodes.OrEmpty();
    }

    /// <summary>Gets where an editor can find the JSON schema, or <c>null</c> when none is named.</summary>
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
    /// The document's top, which has no heading and so is not an anchor. A runtime starts here
    /// rather than assuming node 0.
    /// </remarks>
    [JsonPropertyOrder(3)]
    [JsonPropertyName("entry")]
    public int Entry { get; }

    /// <summary>
    /// Gets each scene's slug, mapped to the index of the node it starts at: the targets a jump
    /// may name, and the named conversations a game may start at instead of the top.
    /// </summary>
    /// <remarks>
    /// The heading <c>## Discuss Bob's photo</c> gives the slug <c>discuss-bobs-photo</c>.
    /// <para>
    /// Sorted, because a lookup table's order carries no meaning but a golden file needs one:
    /// sorting makes a playbook byte-identical whatever order the anchors were added in. Sorted
    /// <em>ordinally</em>, so that holds on any machine — the default comparer follows the
    /// current culture, which puts "a" before "B" in one place and after it in another.
    /// </para>
    /// </remarks>
    [UnorderedEquality]
    [JsonPropertyOrder(4)]
    [JsonPropertyName("anchors")]
    public ImmutableSortedDictionary<string, int> Anchors { get; }

    /// <summary>Gets everybody who speaks here.</summary>
    [OrderedEquality]
    [JsonPropertyOrder(5)]
    [JsonPropertyName("speakers")]
    public ImmutableArray<PlaybookSpeaker> Speakers { get; }

    /// <summary>Gets the steps of a playthrough, each at its own index.</summary>
    [OrderedEquality]
    [JsonPropertyOrder(6)]
    [JsonPropertyName("nodes")]
    public ImmutableArray<Node> Nodes { get; }
}

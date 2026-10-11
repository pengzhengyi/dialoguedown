using System.Collections.Immutable;
using DialogueDown.Playbook;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Speakers;

namespace DialogueDown.TestSupport;

/// <summary>
/// Builds a playbook document, its format, and its speakers with sensible defaults, so a test
/// states only what it is about and a change to one constructor touches this file rather than
/// every test.
/// </summary>
/// <remarks>
/// The nodes a document holds come from <see cref="PlaybookNodeFactory"/>, and the speech inside
/// them from <see cref="SpeechFactory"/>.
/// </remarks>
public static class PlaybookFactory
{
    /// <summary>
    /// A playbook for <c>chapter-01.dialogue.md</c> that ends at once, unless the test says
    /// otherwise.
    /// </summary>
    public static PlaybookDocument Document(
        PlaybookFormat? format = null,
        string script = "chapter-01.dialogue.md",
        int entry = 0,
        IEnumerable<(string Slug, int Node)>? anchors = null,
        IEnumerable<PlaybookSpeaker>? speakers = null,
        IEnumerable<Node>? nodes = null,
        string? schema = null) =>
        new(
            format ?? Format(),
            script,
            entry,
            Table(anchors ?? []),
            [.. speakers ?? []],
            [.. nodes ?? [new EndNode(0)]],
            schema);

    /// <summary>
    /// The format a playbook declares: version 0, needing only the core, unless the test says
    /// otherwise.
    /// </summary>
    public static PlaybookFormat Format(
        int version = 0,
        IEnumerable<string>? requires = null,
        IEnumerable<string>? uses = null) =>
        new(version, [.. requires ?? [Capabilities.Core]], [.. uses ?? []]);

    /// <summary>One speaker, Alice unless the test says otherwise.</summary>
    public static PlaybookSpeaker Speaker(
        string? id = null,
        string? name = "Alice",
        bool @default = false,
        IEnumerable<SpeakerTag>? tags = null) =>
        new(id, name, @default, [.. tags ?? []]);

    /// <summary>
    /// The playbook's speakers, in order. A null name is the anonymous speaker every script has.
    /// </summary>
    public static ImmutableArray<PlaybookSpeaker> Speakers(params string?[] names) =>
        [.. names.Select(name => Speaker(name: name))];

    private static ImmutableSortedDictionary<string, int> Table(
        IEnumerable<(string Key, int Node)> entries) =>
        entries.ToImmutableSortedDictionary(entry => entry.Key, entry => entry.Node);
}

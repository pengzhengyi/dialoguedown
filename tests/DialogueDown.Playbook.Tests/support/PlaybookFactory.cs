using System.Collections.Immutable;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Speakers;

namespace DialogueDown.Playbook.Tests.Support;

/// <summary>
/// Builds playbook values with sensible defaults, so a test states only what it is about and a
/// change to one constructor touches this file rather than every test.
/// </summary>
internal static class PlaybookFactory
{
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

    public static PlaybookFormat Format(
        int version = 0,
        IEnumerable<string>? requires = null,
        IEnumerable<string>? uses = null) =>
        new(version, [.. requires ?? [Capabilities.Core]], [.. uses ?? []]);

    public static PlaybookSpeaker Speaker(
        string? id = null, string? name = "Alice", bool @default = false) =>
        new(id, name, @default, []);

    private static ImmutableSortedDictionary<string, int> Table(
        IEnumerable<(string Key, int Node)> entries) =>
        entries.ToImmutableSortedDictionary(entry => entry.Key, entry => entry.Node);
}

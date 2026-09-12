using System.Collections.Immutable;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Speakers;
using DialogueDown.Playbook.Speech;

namespace DialogueDown.Playbook.Tests.Support;

/// <summary>
/// Builds playbook values with sensible defaults, so a test states only what it is about and a
/// change to one constructor touches this file rather than every test.
/// </summary>
/// <remarks>
/// The speech builders always take their words, because a test over speech asserts the exact words
/// that come out, and the assertion reads best beside what went in. Values a test has no interest
/// in — a link's target, an image's source — have defaults.
/// </remarks>
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

    public static TextFragment Text(string text) => new(text);

    public static StyledTextFragment Bold(string text) => Bold(Text(text));

    public static StyledTextFragment Bold(params SpeechFragment[] children) =>
        Styled(SpeechStyle.Bold, children);

    public static StyledTextFragment Italic(string text) => Italic(Text(text));

    public static StyledTextFragment Italic(params SpeechFragment[] children) =>
        Styled(SpeechStyle.Italic, children);

    public static StyledTextFragment Strikethrough(string text) => Strikethrough(Text(text));

    public static StyledTextFragment Strikethrough(params SpeechFragment[] children) =>
        Styled(SpeechStyle.Strikethrough, children);

    public static LinkFragment Link(string target = "#the-old-road", params SpeechFragment[] label) =>
        new(target, [.. label.Length == 0 ? [Text("a road")] : label]);

    public static ImageFragment Image(string source = "art/key.png", params SpeechFragment[] alt) =>
        new(source, [.. alt.Length == 0 ? [Text("a key")] : alt]);

    public static LineBreakFragment LineBreak() => new();

    public static QueryFragment Query(string key) => new(key);

    public static TagFragment Tag(string name, string? value = null, bool reserved = false) =>
        new(name, value, reserved);

    public static DefaultCommandFragment DefaultCommand(string action = "fade out") => new(action);

    public static CustomCommandFragment CustomCommand(string name, params string[] args) =>
        new(name, [.. args]);

    private static StyledTextFragment Styled(SpeechStyle style, SpeechFragment[] children) =>
        new(style, [.. children.Length == 0 ? [Text("styled")] : children]);

    private static ImmutableSortedDictionary<string, int> Table(
        IEnumerable<(string Key, int Node)> entries) =>
        entries.ToImmutableSortedDictionary(entry => entry.Key, entry => entry.Node);
}

using System.Collections.Immutable;
using DialogueDown.Playbook.Speech;

namespace DialogueDown.Play.Tests.Support;

/// <summary>Shorthand for building the speech fragments a rendering test feeds in.</summary>
internal static class Fragments
{
    public static TextFragment Text(string text) => new(text);

    public static StyledTextFragment Bold(params SpeechFragment[] children) =>
        Styled(SpeechStyle.Bold, children);

    public static StyledTextFragment Italic(params SpeechFragment[] children) =>
        Styled(SpeechStyle.Italic, children);

    public static StyledTextFragment Strikethrough(params SpeechFragment[] children) =>
        Styled(SpeechStyle.Strikethrough, children);

    public static LinkFragment Link(string target, params SpeechFragment[] label) =>
        new(target, label.ToImmutableArray());

    public static ImageFragment Image(string source, params SpeechFragment[] alt) =>
        new(source, alt.ToImmutableArray());

    public static LineBreakFragment LineBreak() => new();

    public static QueryFragment Query(string key) => new(key);

    public static TagFragment Tag(string name, string? value = null, bool reserved = false) =>
        new(name, value, reserved);

    public static DefaultCommandFragment DefaultCommand(string action) => new(action);

    public static CustomCommandFragment CustomCommand(string name, params string[] args) =>
        new(name, args.ToImmutableArray());

    private static StyledTextFragment Styled(SpeechStyle style, SpeechFragment[] children) =>
        new(style, children.ToImmutableArray());
}

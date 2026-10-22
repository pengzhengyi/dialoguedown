using DialogueDown.Playbook.Speech;

namespace DialogueDown.TestSupport;

/// <summary>
/// Builds the pieces of speech a playbook line, option, or effect holds.
/// </summary>
/// <remarks>
/// A builder for words takes them, because a test over speech asserts the exact words that come
/// out, and the assertion reads best beside what went in. Values a test has no interest in — a
/// link's target, an image's source — have defaults.
/// </remarks>
public static class SpeechFactory
{
    /// <summary>Words a writer typed, including any braces they meant literally.</summary>
    public static TextFragment Text(string text) => new(text);

    /// <summary>Words in bold.</summary>
    public static StyledTextFragment Bold(string text) => Bold(Text(text));

    /// <summary>Bold around other pieces, which may be styled in turn.</summary>
    public static StyledTextFragment Bold(params SpeechFragment[] children) =>
        Styled(SpeechStyle.Bold, children);

    /// <summary>Words in italics.</summary>
    public static StyledTextFragment Italic(string text) => Italic(Text(text));

    /// <summary>Italics around other pieces, which may be styled in turn.</summary>
    public static StyledTextFragment Italic(params SpeechFragment[] children) =>
        Styled(SpeechStyle.Italic, children);

    /// <summary>Words struck through.</summary>
    public static StyledTextFragment Strikethrough(string text) => Strikethrough(Text(text));

    /// <summary>A strikethrough around other pieces, which may be styled in turn.</summary>
    public static StyledTextFragment Strikethrough(params SpeechFragment[] children) =>
        Styled(SpeechStyle.Strikethrough, children);

    /// <summary>A link: <c>[a road](#the-old-road)</c> unless the test says otherwise.</summary>
    public static LinkFragment Link(string target = "#the-old-road", params SpeechFragment[] label) =>
        new(target, [.. label.Length == 0 ? [Text("a road")] : label]);

    /// <summary>An image: <c>![a key](art/key.png)</c> unless the test says otherwise.</summary>
    public static ImageFragment Image(string source = "art/key.png", params SpeechFragment[] alt) =>
        new(source, [.. alt.Length == 0 ? [Text("a key")] : alt]);

    /// <summary>A soft line break: where the source wrapped within one line of speech.</summary>
    public static LineBreakFragment LineBreak() => new();

    /// <summary>A question the game answers at play time, such as <c>`"Gold"`</c>.</summary>
    public static QueryFragment Query(string key) => new(key);

    /// <summary>An annotation for the host, which the writer coins unless it is reserved.</summary>
    public static TagFragment Tag(string name, string? value = null, bool reserved = false) =>
        new(name, value, reserved);

    /// <summary>One of the commands the host already knows how to perform, such as <c>`("fade out")`</c>.</summary>
    public static DefaultCommandFragment DefaultCommand(string action = "fade out") => new(action);

    /// <summary>A command the host supplies, with the arguments the script passes it.</summary>
    public static CustomCommandFragment CustomCommand(string name, params string[] args) =>
        new(name, [.. args]);

    /// <summary>Any style around other pieces, for a test that takes the style as a value; "styled" when given none.</summary>
    public static StyledTextFragment Styled(SpeechStyle style, params SpeechFragment[] children) =>
        new(style, [.. children.Length == 0 ? [Text("styled")] : children]);
}

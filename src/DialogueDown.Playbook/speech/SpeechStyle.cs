using System.Text.Json.Serialization;

namespace DialogueDown.Playbook.Speech;

/// <summary>
/// How a stretch of speech is emphasized.
/// </summary>
/// <remarks>
/// Written to JSON by fixed names (<c>italic</c>, <c>bold</c>, <c>strikethrough</c>), so renaming
/// a member does not change the format.
/// </remarks>
[JsonConverter(typeof(SpeechStyleConverter))]
public enum SpeechStyle
{
    /// <summary>Emphasis, written with single asterisks or underscores.</summary>
    Italic,

    /// <summary>Strong emphasis, written with double asterisks or underscores.</summary>
    Bold,

    /// <summary>Struck-through text, written with double tildes.</summary>
    Strikethrough,
}

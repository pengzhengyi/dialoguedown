using System.Text.Json.Serialization;

namespace DialogueDown.Playbook;

/// <summary>
/// How a run of speech is emphasized.
/// </summary>
/// <remarks>
/// Each member's wire name is pinned explicitly, for the reason a property carries a
/// <see cref="JsonPropertyNameAttribute"/>: the value is a public contract, and deriving it from
/// the C# member name would let a rename change the format silently.
/// </remarks>
[JsonConverter(typeof(StringOnlyEnumConverter<SpeechStyle>))]
public enum SpeechStyle
{
    /// <summary>Emphasis, written with single asterisks or underscores.</summary>
    [JsonStringEnumMemberName("italic")]
    Italic,

    /// <summary>Strong emphasis, written with double asterisks or underscores.</summary>
    [JsonStringEnumMemberName("bold")]
    Bold,

    /// <summary>Struck-through text, written with double tildes.</summary>
    [JsonStringEnumMemberName("strikethrough")]
    Strikethrough,
}

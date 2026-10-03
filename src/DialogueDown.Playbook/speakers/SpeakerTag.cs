using System.Text.Json.Serialization;
using DialogueDown.Playbook.Common;

namespace DialogueDown.Playbook.Speakers;

/// <summary>
/// An annotation on a speaker, such as a portrait or a voice a host binds.
/// </summary>
/// <remarks>
/// Written after the speaker's name, such as <c>#mood=happy</c> in
/// <c>Bob @B #mood=happy: Thank you.</c> A tag among the words of a line is a
/// <see cref="DialogueDown.Playbook.Speech.TagFragment"/> instead.
/// </remarks>
/// <param name="Name">The tag's name.</param>
/// <param name="Value">The tag's value, or <c>null</c> when it carries none.</param>
/// <param name="Reserved">Whether the language reserves this name rather than the writer coining it.</param>
public sealed record SpeakerTag(string Name, string? Value, bool Reserved)
{
    /// <summary>Gets the tag's name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; } = Name.AssertNotNull(nameof(Name));

    /// <summary>Gets the tag's value, or <c>null</c> when it carries none.</summary>
    [JsonPropertyName("value")]
    public string? Value { get; } = Value;

    /// <summary>Gets a value indicating whether the language reserves this name.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonPropertyName("reserved")]
    public bool Reserved { get; } = Reserved;
}

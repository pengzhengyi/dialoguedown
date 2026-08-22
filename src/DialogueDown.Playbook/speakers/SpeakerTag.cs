using System.Text.Json.Serialization;
using DialogueDown.Playbook.Common;

namespace DialogueDown.Playbook.Speakers;

/// <summary>
/// An annotation on a speaker, such as a portrait or a voice a host binds.
/// </summary>
/// <remarks>
/// Shaped like a tag in speech but a separate type, because this one annotates a *speaker*
/// rather than a point in a line — typing it as a fragment would put it in a union it can
/// never appear in.
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

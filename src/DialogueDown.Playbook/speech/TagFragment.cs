using System.Text.Json.Serialization;

namespace DialogueDown.Playbook;

/// <summary>
/// An annotation attached at a point in a line, for a host to recognize or act on.
/// </summary>
/// <param name="Name">The tag's name.</param>
/// <param name="Value">The tag's value, or <c>null</c> when it carries none.</param>
/// <param name="Reserved">Whether the language reserves this name rather than the writer coining it.</param>
public sealed record TagFragment(string Name, string? Value, bool Reserved) : SpeechFragment
{
    /// <summary>
    /// Gets the tag's name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; } = Name.AssertNotNull(nameof(Name));

    /// <summary>
    /// Gets the tag's value, or <c>null</c> when it carries none.
    /// </summary>
    [JsonPropertyName("value")]
    public string? Value { get; } = Value;

    /// <summary>
    /// Gets a value indicating whether the language reserves this name.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonPropertyName("reserved")]
    public bool Reserved { get; } = Reserved;
}

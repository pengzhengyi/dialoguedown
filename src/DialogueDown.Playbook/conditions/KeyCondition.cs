using System.Text.Json.Serialization;

namespace DialogueDown.Playbook;

/// <summary>
/// A condition the world answers by key — the only shape a condition takes today.
/// </summary>
/// <param name="Key">What the world is asked.</param>
public sealed record KeyCondition(string Key) : Condition
{
    /// <summary>
    /// Gets what the world is asked.
    /// </summary>
    [JsonPropertyName("key")]
    public string Key { get; } = Key.AssertNotNull(nameof(Key));
}

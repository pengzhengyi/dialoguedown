using System.Text.Json.Serialization;
using DialogueDown.Playbook.Common;

namespace DialogueDown.Playbook.Conditions;

/// <summary>
/// A condition the world answers by key.
/// </summary>
/// <remarks>
/// <code>
/// `Alice.HasKey?` Alice: I have the key.
/// </code>
/// gives a line guarded by the key <c>Alice.HasKey</c>.
/// </remarks>
/// <param name="Key">The key the world is asked about.</param>
public sealed record KeyCondition(string Key) : Condition
{
    /// <summary>
    /// Gets the key the world is asked about.
    /// </summary>
    [JsonPropertyName("key")]
    public string Key { get; } = Key.AssertNotNull(nameof(Key));
}

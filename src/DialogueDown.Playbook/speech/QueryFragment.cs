using System.Text.Json.Serialization;

namespace DialogueDown.Playbook;

/// <summary>
/// A read of game state spliced into speech — the runner resolves it before the line is said.
/// </summary>
/// <param name="Key">What the world is asked for.</param>
public sealed record QueryFragment(string Key) : SpeechFragment
{
    /// <summary>
    /// Gets what the world is asked for.
    /// </summary>
    [JsonPropertyName("key")]
    public string Key { get; } = Key.AssertNotNull(nameof(Key));
}

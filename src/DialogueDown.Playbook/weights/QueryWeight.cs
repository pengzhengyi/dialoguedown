using System.Text.Json.Serialization;
using DialogueDown.Playbook.Common;

namespace DialogueDown.Playbook.Weights;

/// <summary>
/// A percentage the world supplies at play time, so odds can follow game state.
/// </summary>
/// <param name="Key">What the world is asked for.</param>
public sealed record QueryWeight(string Key) : ChoiceWeight
{
    /// <summary>
    /// Gets what the world is asked for.
    /// </summary>
    /// <remarks>
    /// The playbook carries only the question. Resolving it to a number, and checking that
    /// number is finite and not negative, belongs to the runtime that asks.
    /// </remarks>
    [JsonPropertyName("key")]
    public string Key { get; } = Key.AssertNotNull(nameof(Key));
}

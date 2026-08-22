using System.Text.Json.Serialization;
using DialogueDown.Playbook.Common;

namespace DialogueDown.Playbook.Speech;

/// <summary>
/// A command written as a plain phrase, which the host performs however it chooses.
/// </summary>
/// <param name="Action">The phrase the writer wrote.</param>
public sealed record DefaultCommandFragment(string Action) : SpeechFragment
{
    /// <summary>
    /// Gets the phrase the writer wrote.
    /// </summary>
    [JsonPropertyName("action")]
    public string Action { get; } = Action.AssertNotNull(nameof(Action));
}

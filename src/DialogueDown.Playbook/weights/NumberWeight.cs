using System.Text.Json.Serialization;

namespace DialogueDown.Playbook;

/// <summary>
/// A percentage the writer fixed in the script.
/// </summary>
/// <param name="Percentage">The share this option takes.</param>
public sealed record NumberWeight(double Percentage) : ChoiceWeight
{
    /// <summary>
    /// Gets the share this option takes.
    /// </summary>
    [JsonPropertyName("percentage")]
    public double Percentage { get; } = Percentage;
}

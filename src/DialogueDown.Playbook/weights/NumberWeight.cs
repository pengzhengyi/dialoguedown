using System.Text.Json.Serialization;

namespace DialogueDown.Playbook.Weights;

/// <summary>
/// A percentage the writer fixed in the script.
/// </summary>
/// <param name="Percentage">The share this option takes, in percent: <c>`70%`</c> gives 70.</param>
public sealed record NumberWeight(double Percentage) : ChoiceWeight
{
    /// <summary>
    /// Gets the share this option takes, in percent: <c>`70%`</c> gives 70.
    /// </summary>
    [JsonPropertyName("percentage")]
    public double Percentage { get; } = Percentage;
}

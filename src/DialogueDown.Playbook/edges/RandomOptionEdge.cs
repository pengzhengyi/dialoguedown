using System.Text.Json.Serialization;
using DialogueDown.Playbook.Conditions;
using DialogueDown.Playbook.Weights;

namespace DialogueDown.Playbook.Edges;

/// <summary>
/// One arm of a random choice, carrying the odds the engine draws against.
/// </summary>
/// <remarks>It has no label: the engine picks this arm, so nobody is shown a menu.</remarks>
/// <param name="Target">The node this option leads to.</param>
/// <param name="Weight">How likely this option is to be drawn.</param>
/// <param name="Condition">What must hold for the option to be in the pool, or <c>null</c>.</param>
public sealed record RandomOptionEdge(int Target, ChoiceWeight Weight, Condition? Condition)
    : Edge(Target)
{
    /// <summary>
    /// Gets how likely this option is to be drawn.
    /// </summary>
    [JsonPropertyOrder(2)]
    [JsonPropertyName("weight")]
    public ChoiceWeight Weight { get; } = Weight ?? throw new ArgumentNullException(nameof(Weight));

    /// <summary>
    /// Gets what must hold for the option to be in the pool, or <c>null</c> when it always is.
    /// </summary>
    [JsonPropertyOrder(3)]
    [JsonPropertyName("condition")]
    public Condition? Condition { get; } = Condition;
}

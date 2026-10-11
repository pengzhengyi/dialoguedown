using System.Text.Json.Serialization;
using DialogueDown.Playbook.Conditions;

namespace DialogueDown.Playbook.Edges;

/// <summary>
/// One arm of a block condition.
/// </summary>
/// <param name="Target">The node this arm leads to.</param>
/// <param name="Condition">What must hold for the arm to be taken, or <c>null</c> for an else.</param>
public sealed record BranchEdge(int Target, Condition? Condition)
    : Edge(Target), IConditional
{
    /// <summary>
    /// Gets what must hold for the arm to be taken, or <c>null</c> for a final else.
    /// </summary>
    [JsonPropertyOrder(2)]
    [JsonPropertyName("condition")]
    public Condition? Condition { get; } = Condition;
}

using System.Text.Json.Serialization;
using DialogueDown.Playbook.Conditions;

namespace DialogueDown.Playbook.Edges;

/// <summary>
/// A jump that does not return: control transfers to the target and reading order does not resume.
/// </summary>
/// <param name="Target">The node control transfers to.</param>
/// <param name="Condition">What must hold for the jump to fire, or <c>null</c>.</param>
public sealed record DivertEdge(int Target, Condition? Condition) : Edge(Target)
{
    /// <summary>
    /// Gets what must hold for the jump to fire, or <c>null</c> when it always does.
    /// </summary>
    [JsonPropertyOrder(3)]
    [JsonPropertyName("condition")]
    public Condition? Condition { get; } = Condition;
}

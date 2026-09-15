using System.Collections.Immutable;
using System.Text.Json.Serialization;
using DialogueDown.Playbook.Common;
using DialogueDown.Playbook.Conditions;
using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Speech;
using Generator.Equals;

namespace DialogueDown.Playbook.Nodes;

/// <summary>
/// An effect-only line: something the host performs, attributed to nobody.
/// </summary>
/// <param name="Id">This node's position in the node list.</param>
/// <param name="Effects">What the host performs here.</param>
/// <param name="Condition">What must hold for the effects to fire, or <c>null</c>.</param>
/// <param name="Out">The ways out of this node.</param>
[Equatable]
public sealed partial record ControlNode(
    int Id,
    ImmutableArray<SpeechFragment> Effects,
    Condition? Condition,
    ImmutableArray<Edge> Out) : Node(Id, Out), IConditional
{
    /// <summary>
    /// Gets what the host performs here.
    /// </summary>
    [OrderedEquality]
    [JsonPropertyOrder(4)]
    [JsonPropertyName("effects")]
    public ImmutableArray<SpeechFragment> Effects { get; } = Effects.OrEmpty();

    /// <summary>
    /// Gets what must hold for the effects to fire, or <c>null</c> when they always do.
    /// </summary>
    [JsonPropertyOrder(5)]
    [JsonPropertyName("condition")]
    public Condition? Condition { get; } = Condition;
}

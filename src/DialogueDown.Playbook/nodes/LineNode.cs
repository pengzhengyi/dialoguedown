using System.Collections.Immutable;
using System.Text.Json.Serialization;
using DialogueDown.Playbook.Common;
using DialogueDown.Playbook.Conditions;
using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Speech;

namespace DialogueDown.Playbook.Nodes;

/// <summary>
/// A line somebody says.
/// </summary>
/// <param name="Id">This node's position in the node list.</param>
/// <param name="Speaker">Who says it, by index into the playbook's speakers.</param>
/// <param name="Speech">What is said.</param>
/// <param name="Condition">What must hold for the line to play, or <c>null</c>.</param>
/// <param name="Out">The ways out of this node.</param>
public sealed record LineNode(
    int Id,
    int Speaker,
    ImmutableArray<SpeechFragment> Speech,
    Condition? Condition,
    ImmutableArray<Edge> Out) : Node(Id, Out)
{
    /// <summary>
    /// Gets who says the line, by index into the playbook's speakers.
    /// </summary>
    [JsonPropertyOrder(2)]
    [JsonPropertyName("speaker")]
    public int Speaker { get; } = Speaker.AssertNotNegative(nameof(Speaker));

    /// <summary>
    /// Gets what is said.
    /// </summary>
    [JsonPropertyOrder(4)]
    [JsonPropertyName("speech")]
    public ImmutableArray<SpeechFragment> Speech { get; } = Speech.OrEmpty();

    /// <summary>
    /// Gets what must hold for the line to play, or <c>null</c> when it always does.
    /// </summary>
    [JsonPropertyOrder(5)]
    [JsonPropertyName("condition")]
    public Condition? Condition { get; } = Condition;
}

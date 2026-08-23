using System.Collections.Immutable;
using System.Text.Json.Serialization;
using DialogueDown.Playbook.Edges;

namespace DialogueDown.Playbook.Nodes;

/// <summary>
/// A menu the player picks from; its options are the ways out.
/// </summary>
/// <param name="Id">This node's position in the node list.</param>
/// <param name="Ordered">Whether the options are numbered rather than bulleted.</param>
/// <param name="Out">The options.</param>
public sealed record ChoiceNode(int Id, bool Ordered, ImmutableArray<Edge> Out) : Node(Id, Out)
{
    /// <summary>
    /// Gets whether the options are numbered rather than bulleted.
    /// </summary>
    [JsonPropertyOrder(2)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonPropertyName("ordered")]
    public bool Ordered { get; } = Ordered;
}

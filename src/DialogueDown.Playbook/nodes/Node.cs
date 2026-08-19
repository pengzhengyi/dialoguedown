using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace DialogueDown.Playbook;

/// <summary>
/// One step of a playthrough: something happens here, then an edge leads on.
/// </summary>
/// <remarks>
/// Every node has an identity and its ways out, so the base carries both. Field order is stated
/// rather than inherited, because a serializer writes a derived type's own properties before its
/// base's.
/// </remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = PlaybookJson.Discriminator)]
[JsonDerivedType(typeof(LineNode), NodeKinds.Line)]
[JsonDerivedType(typeof(ChoiceNode), NodeKinds.Choice)]
[JsonDerivedType(typeof(RandomChoiceNode), NodeKinds.RandomChoice)]
[JsonDerivedType(typeof(BranchNode), NodeKinds.Branch)]
[JsonDerivedType(typeof(ControlNode), NodeKinds.Control)]
[JsonDerivedType(typeof(EndNode), NodeKinds.End)]
public abstract record Node
{
    private protected Node(int id, ImmutableArray<Edge> ways)
    {
        Id = id.AssertNotNegative(nameof(id));
        Out = ways.OrEmpty();
    }

    /// <summary>
    /// Gets this node's position in the playbook's node list, which its index must match.
    /// </summary>
    [JsonPropertyOrder(1)]
    [JsonPropertyName("id")]
    public int Id { get; }

    /// <summary>
    /// Gets the ways out of this node.
    /// </summary>
    [JsonPropertyOrder(6)]
    [JsonPropertyName("out")]
    public ImmutableArray<Edge> Out { get; }
}

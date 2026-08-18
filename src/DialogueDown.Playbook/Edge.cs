using System.Text.Json.Serialization;

namespace DialogueDown.Playbook;

/// <summary>
/// A way out of a node, and the node it leads to.
/// </summary>
/// <remarks>
/// Every edge has a target, so the base carries it; what differs between kinds is why the edge
/// is taken — a player picked it, a condition allowed it, or reading order simply continued.
/// </remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = PlaybookJson.Discriminator)]
[JsonDerivedType(typeof(SuccessionEdge), EdgeKinds.Succession)]
[JsonDerivedType(typeof(OptionEdge), EdgeKinds.Option)]
[JsonDerivedType(typeof(RandomOptionEdge), EdgeKinds.RandomOption)]
[JsonDerivedType(typeof(BranchEdge), EdgeKinds.Branch)]
[JsonDerivedType(typeof(DivertEdge), EdgeKinds.Divert)]
public abstract record Edge
{
    private protected Edge(int target) => Target = target.AssertNotNegative(nameof(target));

    /// <summary>
    /// Gets the node this edge leads to, as an index into the playbook's node list.
    /// </summary>
    /// <remarks>
    /// Field order is stated rather than inherited: a serializer writes a derived type's own
    /// properties before its base's, which would bury the target under whatever each kind adds.
    /// Pinning it keeps every edge reading the same way and keeps golden files stable when a
    /// property moves.
    /// </remarks>
    [JsonPropertyOrder(1)]
    [JsonPropertyName("target")]
    public int Target { get; }
}

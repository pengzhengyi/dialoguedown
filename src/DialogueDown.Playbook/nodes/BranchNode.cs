using System.Collections.Immutable;
using DialogueDown.Playbook.Edges;

namespace DialogueDown.Playbook.Nodes;

/// <summary>
/// A block condition fanning out to its arms; the arms carry the conditions and their order.
/// </summary>
/// <param name="Id">This node's position in the node list.</param>
/// <param name="Out">The arms, in the order they are tried.</param>
public sealed record BranchNode(int Id, ImmutableArray<Edge> Out) : Node(Id, Out);

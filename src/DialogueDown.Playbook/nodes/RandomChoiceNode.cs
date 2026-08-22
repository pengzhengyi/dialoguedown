using System.Collections.Immutable;
using DialogueDown.Playbook.Edges;

namespace DialogueDown.Playbook.Nodes;

/// <summary>
/// A choice the engine draws instead of showing, weighted by its arms.
/// </summary>
/// <param name="Id">This node's position in the node list.</param>
/// <param name="Out">The arms, each carrying its odds.</param>
public sealed record RandomChoiceNode(int Id, ImmutableArray<Edge> Out) : Node(Id, Out);

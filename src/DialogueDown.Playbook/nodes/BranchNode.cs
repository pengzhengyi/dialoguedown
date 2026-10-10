using System.Collections.Immutable;
using DialogueDown.Playbook.Edges;

namespace DialogueDown.Playbook.Nodes;

/// <summary>
/// A block condition fanning out to its arms; the arms carry the conditions.
/// </summary>
/// <remarks>
/// <code>
/// &gt; `if` `Bob.Affection?`
/// &gt;
/// &gt; Bob: Come with me.
/// &gt;
/// &gt; `else`
/// &gt;
/// &gt; Bob: Perhaps another day.
/// </code>
/// gives a branch with two arms: one guarded by <c>Bob.Affection</c>, then the <c>else</c>.
/// </remarks>
/// <param name="Id">This node's position in the node list.</param>
/// <param name="Out">
/// The arms, in the order they are tried, and the succession to fall through to when there is one.
/// </param>
public sealed record BranchNode(int Id, ImmutableArray<Edge> Out) : Node(Id, Out);

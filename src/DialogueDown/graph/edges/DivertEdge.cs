using DialogueDown.Script.Ast;

namespace DialogueDown.Graph.Edges;

/// <summary>
/// A jump edge: control moves to the <see cref="Edge.Target"/> and does not return. An
/// unconditional divert is always taken, so its source node does not fall through; a conditional
/// divert fires only when its condition reads true.
/// </summary>
/// <remarks>
/// The <see cref="Label"/> is the link text the writer gave the jump, and the edge is the only
/// place it is kept:
/// <code>
/// Alice: Let's go. =&gt; [To the market](#the-market)
/// </code>
/// gives a divert to the scene <c>the-market</c> labeled <c>To the market</c>.
/// </remarks>
/// <param name="Target">Where control goes.</param>
/// <param name="Label">What the writer called the jump, as written.</param>
/// <param name="Condition">What must hold for the divert to fire, or <c>null</c>.</param>
internal sealed record DivertEdge(
    NodeId Target, IReadOnlyList<InlineFragment> Label, Condition? Condition = null)
    : Edge(Target), IConditionalEdge;

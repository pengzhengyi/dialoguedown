using DialogueDown.Script.Ast;

namespace DialogueDown.Graph.Edges;

/// <summary>
/// A non-returning jump edge: control transfers to the <see cref="Edge.Target"/> and does not
/// return. An unconditional divert is taken unconditionally, so its source node does not fall through;
/// a conditional divert fires only when its condition reads true.
/// </summary>
/// <remarks>
/// Unlike an <see cref="OptionEdge"/>, whose displayed text is the speech of the node it leads to,
/// a divert carries its own <see cref="Label"/>. The jump it came from is not kept anywhere else,
/// so the edge is the only place the words the writer chose can still be found.
/// </remarks>
/// <param name="Target">Where control goes.</param>
/// <param name="Label">What the writer called the jump, as written.</param>
/// <param name="Condition">What must hold for the divert to fire, or <c>null</c>.</param>
internal sealed record DivertEdge(
    NodeId Target, IReadOnlyList<InlineFragment> Label, Condition? Condition = null)
    : Edge(Target), IConditionalEdge;

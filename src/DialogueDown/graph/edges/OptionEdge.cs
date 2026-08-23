using DialogueDown.Script.Ast;

namespace DialogueDown.Graph.Edges;

/// <summary>
/// One arm of a <see cref="Nodes.ChoiceNode"/>: the body it leads to, and the <see cref="Label"/>
/// shown when the option is offered. A conditional option is offered only when its condition
/// reads true.
/// </summary>
/// <remarks>
/// The label is the speech of the arm's first line, carried here rather than read back off the
/// node the option leads to. Only this pass knows which nodes belong to which arm — an option
/// with an empty body leads straight to whatever follows the choice, whose speech is somebody
/// else's line entirely.
/// </remarks>
/// <param name="Target">The first node of the arm's body.</param>
/// <param name="Label">The words shown for this option.</param>
/// <param name="Condition">What must hold for the option to be offered, or <c>null</c>.</param>
internal sealed record OptionEdge(
    NodeId Target, IReadOnlyList<InlineFragment> Label, Condition? Condition = null)
    : Edge(Target), IConditionalEdge;

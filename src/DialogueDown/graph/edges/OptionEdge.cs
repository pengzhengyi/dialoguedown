using DialogueDown.Script.Ast;

namespace DialogueDown.Graph.Edges;

/// <summary>
/// One arm of a <see cref="Nodes.ChoiceNode"/>: the body it leads to, and the <see cref="Label"/>
/// shown when the option is offered. A conditional option is offered only when its condition
/// reads true.
/// </summary>
/// <remarks>
/// The label comes from the arm's first block: the words its line speaks or, when that block only
/// jumps, the jump's text. It is carried on the edge because an arm with an empty body leads
/// straight to whatever follows the choice, a line that is not the option's own.
/// </remarks>
/// <param name="Target">The first node of the arm's body.</param>
/// <param name="Label">The words shown for this option.</param>
/// <param name="Condition">What must hold for the option to be offered, or <c>null</c>.</param>
internal sealed record OptionEdge(
    NodeId Target, IReadOnlyList<InlineFragment> Label, Condition? Condition = null)
    : Edge(Target), IConditionalEdge;

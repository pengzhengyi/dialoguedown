using DialogueDown.Common;
using DialogueDown.Graph.Nodes;
using DialogueDown.Script.Ast;

namespace DialogueDown.Graph.Builder;

/// <summary>
/// An effect-only control block under construction: the ordered game calls it runs, combined with
/// the edges graph passes accumulate on it.
/// </summary>
internal sealed class ControlNodeDraft : NodeDraft, IConditionalNode
{
    private readonly IReadOnlyList<GameCall> _effects;

    public ControlNodeDraft(
        NodeId id, SourceSpan span, IReadOnlyList<GameCall> effects, Condition? condition = null)
        : base(id, span)
    {
        ArgumentNullException.ThrowIfNull(effects);
        _effects = effects;
        Condition = condition;
    }

    /// <inheritdoc/>
    public Condition? Condition { get; }

    /// <inheritdoc/>
    /// <remarks>
    /// A condition may skip the node whole, taking any divert it holds with it, so the fall-through is
    /// the route left when the condition reads false.
    /// </remarks>
    public override bool LeavesUnconditionally() =>
        Condition is null && base.LeavesUnconditionally();

    protected override DialogueNode CreateNode() =>
        new ControlNode(Id, Span, _effects, Out.ToArray(), Condition);
}

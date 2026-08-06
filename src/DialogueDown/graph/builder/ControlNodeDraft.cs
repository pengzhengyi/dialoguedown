using DialogueDown.Common;
using DialogueDown.Graph.Nodes;
using DialogueDown.Script.Ast;

namespace DialogueDown.Graph.Builder;

/// <summary>
/// An effect-only control block under construction: the ordered game calls it runs, combined with
/// the edges graph passes accumulate on it.
/// </summary>
internal sealed class ControlNodeDraft : NodeDraft, IGuardedNode
{
    private readonly IReadOnlyList<GameCall> _effects;

    public ControlNodeDraft(
        NodeId id, SourceSpan span, IReadOnlyList<GameCall> effects, Condition? guard = null)
        : base(id, span)
    {
        ArgumentNullException.ThrowIfNull(effects);
        _effects = effects;
        Guard = guard;
    }

    /// <inheritdoc/>
    public Condition? Guard { get; }

    /// <inheritdoc/>
    /// <remarks>
    /// A guard may skip the node whole, taking any divert it holds with it, so the fall-through is
    /// the route left when the guard reads false.
    /// </remarks>
    public override bool LeavesUnconditionally() =>
        Guard is null && base.LeavesUnconditionally();

    protected override DialogueNode CreateNode() =>
        new ControlNode(Id, Span, _effects, Out.ToArray(), Guard);
}

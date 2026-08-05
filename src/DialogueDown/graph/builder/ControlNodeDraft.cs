using DialogueDown.Graph.Nodes;
using DialogueDown.Script.Ast;

namespace DialogueDown.Graph.Builder;

/// <summary>
/// An effect-only control block under construction: the ordered game calls it runs, combined with
/// the edges graph passes accumulate on it.
/// </summary>
internal sealed class ControlNodeDraft : NodeDraft
{
    private readonly IReadOnlyList<GameCall> _effects;

    public ControlNodeDraft(NodeId id, IReadOnlyList<GameCall> effects)
        : base(id)
    {
        ArgumentNullException.ThrowIfNull(effects);
        _effects = effects;
    }

    protected override DialogueNode CreateNode() => new ControlNode(Id, _effects, Out.ToArray());
}

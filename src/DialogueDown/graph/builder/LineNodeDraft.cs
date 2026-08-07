using DialogueDown.Common;
using DialogueDown.Graph.Nodes;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Semantics;

namespace DialogueDown.Graph.Builder;

/// <summary>
/// A spoken line under construction: its resolved speaker and displayable speech, combined with
/// the edges accumulated by graph passes when frozen.
/// </summary>
internal sealed class LineNodeDraft : NodeDraft, IGuardedNode
{
    private readonly SpeakerSymbol _speaker;
    private readonly IReadOnlyList<InlineFragment> _speech;

    public LineNodeDraft(
        NodeId id,
        SourceSpan span,
        SpeakerSymbol speaker,
        IReadOnlyList<InlineFragment> speech,
        Condition? guard = null)
        : base(id, span)
    {
        ArgumentNullException.ThrowIfNull(speaker);
        ArgumentNullException.ThrowIfNull(speech);
        _speaker = speaker;
        _speech = speech;
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
        new LineNode(Id, Span, _speaker, _speech, Out.ToArray(), Guard);
}

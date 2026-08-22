using DialogueDown.Common;
using DialogueDown.Graph.Edges;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Semantics;

namespace DialogueDown.Graph.Nodes;

/// <summary>
/// A spoken line: its resolved <see cref="Speaker"/> and the displayable <see cref="Speech"/>
/// fragments they say. Its <see cref="Effects"/> are the game calls embedded in that speech,
/// in order — the calls the line runs when it plays. A <see cref="Condition"/> decides whether it
/// is spoken at all; control continues past it either way.
/// </summary>
/// <remarks>
/// A jump the line ended in is not part of its speech. By the time a node exists the jump is the
/// divert leaving it, carrying the label the writer gave it, so keeping it here as well would
/// say one thing twice.
/// </remarks>
internal sealed record LineNode(
    NodeId Id,
    SourceSpan Span,
    SpeakerSymbol Speaker,
    IReadOnlyList<InlineFragment> Speech,
    IReadOnlyList<Edge> Out,
    Condition? Condition = null) : DialogueNode(Id, Span, Out), IConditionalNode
{
    /// <summary>The game calls in the line's speech, in order — its effects.</summary>
    public IReadOnlyList<GameCall> Effects => [.. Speech.OfType<GameCall>()];
}

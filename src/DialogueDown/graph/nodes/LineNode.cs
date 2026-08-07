using DialogueDown.Common;
using DialogueDown.Graph.Edges;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Semantics;

namespace DialogueDown.Graph.Nodes;

/// <summary>
/// A spoken line: its resolved <see cref="Speaker"/> and the displayable <see cref="Speech"/>
/// fragments they say. Its <see cref="Effects"/> are the game calls embedded in that speech,
/// in order — the calls the line runs when it plays. A <see cref="Guard"/> decides whether it
/// is spoken at all; control continues past it either way.
/// </summary>
internal sealed record LineNode(
    NodeId Id,
    SourceSpan Span,
    SpeakerSymbol Speaker,
    IReadOnlyList<InlineFragment> Speech,
    IReadOnlyList<Edge> Out,
    Condition? Guard = null) : DialogueNode(Id, Span, Out), IGuardedNode
{
    /// <summary>The game calls in the line's speech, in order — its effects.</summary>
    public IReadOnlyList<GameCall> Effects => [.. Speech.OfType<GameCall>()];
}

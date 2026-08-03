using DialogueDown.Script.Ast;
using DialogueDown.Script.Semantics;

namespace DialogueDown.Graph;

/// <summary>
/// A spoken line: its resolved <see cref="Speaker"/> and the displayable <see cref="Speech"/>
/// fragments they say. Its <see cref="Effects"/> are the game calls embedded in that speech,
/// in order — the calls the line runs when it plays.
/// </summary>
internal sealed record LineNode(
    NodeId Id,
    SpeakerSymbol Speaker,
    IReadOnlyList<InlineFragment> Speech,
    IReadOnlyList<Edge> Out) : DialogueNode(Id, Out)
{
    /// <summary>The game calls in the line's speech, in order — its effects.</summary>
    public IReadOnlyList<GameCall> Effects => [.. Speech.OfType<GameCall>()];
}

using DialogueDown.Script.Ast;
using DialogueDown.Script.Semantics;

namespace DialogueDown.Graph;

/// <summary>
/// A spoken line: its resolved <see cref="Speaker"/> and the displayable <see cref="Speech"/>
/// fragments they say.
/// </summary>
internal sealed record LineNode(
    NodeId Id,
    SpeakerSymbol Speaker,
    IReadOnlyList<InlineFragment> Speech,
    IReadOnlyList<Edge> Out) : DialogueNode(Id, Out);

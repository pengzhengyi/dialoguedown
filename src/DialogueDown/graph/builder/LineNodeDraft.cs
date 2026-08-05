using DialogueDown.Graph.Nodes;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Semantics;

namespace DialogueDown.Graph.Builder;

/// <summary>
/// A spoken line under construction: its resolved speaker and displayable speech, combined with
/// the edges accumulated by graph passes when frozen.
/// </summary>
internal sealed class LineNodeDraft : NodeDraft
{
    private readonly SpeakerSymbol _speaker;
    private readonly IReadOnlyList<InlineFragment> _speech;

    public LineNodeDraft(
        NodeId id,
        SpeakerSymbol speaker,
        IReadOnlyList<InlineFragment> speech)
        : base(id)
    {
        ArgumentNullException.ThrowIfNull(speaker);
        ArgumentNullException.ThrowIfNull(speech);
        _speaker = speaker;
        _speech = speech;
    }

    protected override DialogueNode CreateNode() =>
        new LineNode(Id, _speaker, _speech, Out.ToArray());
}

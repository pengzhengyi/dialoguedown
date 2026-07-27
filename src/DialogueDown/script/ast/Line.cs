using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// One speech in the dialogue: an optional <see cref="Speaker"/> and the
/// <see cref="Speech"/> they say, an ordered list of fragments. The speaker is absent
/// when the line names none — a default is filled in later. The speech may be empty
/// when the line only declares a speaker. An optional <see cref="Condition"/> guards the
/// line: a conditional line plays only when the condition is true.
/// </summary>
internal sealed record Line(
    Speaker? Speaker, IReadOnlyList<InlineFragment> Speech, SourceSpan Span,
    Condition? Condition = null) : ScriptBlock(Span)
{
    /// <summary>Whether this line is guarded by a condition.</summary>
    public bool IsConditional => Condition is not null;
}

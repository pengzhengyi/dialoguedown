using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// An effect-only block with no speaker: a bare <see cref="Jump"/>, or one or more silent
/// commands, on their own line. Unlike a <see cref="Line"/>, it is not spoken, so no speaker —
/// named or default — is ever attached to it. Its <see cref="Effects"/> are the jump and command
/// fragments in source order, and an optional <see cref="Condition"/> guards them, exactly as a
/// conditional jump is guarded.
/// </summary>
internal sealed record ControlLine(
    IReadOnlyList<InlineFragment> Effects, SourceSpan Span, Condition? Condition = null)
    : ScriptBlock(Span), IConditional;

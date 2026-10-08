using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// An effect-only block with no speaker: a bare <see cref="Jump"/>, or one or more silent
/// commands, on their own line. Unlike a <see cref="Line"/>, it is not spoken, so no speaker —
/// named or default — is ever attached to it. Its <see cref="Effects"/> are the line's fragments
/// in source order: the jumps and commands, and any whitespace or line breaks between them. An
/// optional <see cref="Condition"/> guards them all.
/// </summary>
/// <remarks>
/// <code>
/// `("Open the gate")` =&gt; [Leave](#the-gate)
/// </code>
/// </remarks>
internal sealed record ControlLine(
    IReadOnlyList<InlineFragment> Effects, SourceSpan Span, Condition? Condition = null)
    : ScriptBlock(Span), IConditional;

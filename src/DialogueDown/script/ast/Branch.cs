using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// One arm of a <see cref="ControlBlock"/>: its guarding <see cref="Condition"/> — an <c>if</c> or
/// <c>elseif</c> carries one; the <c>else</c> is null — and the <see cref="Body"/> blocks it plays
/// when taken. Like a <see cref="Choice"/>, a branch is not itself a <see cref="ScriptBlock"/>, and
/// its body may hold a nested control block.
/// </summary>
internal sealed record Branch(
    Condition? Condition, IReadOnlyList<ScriptBlock> Body, SourceSpan Span)
    : ScriptNode(Span), IConditional;

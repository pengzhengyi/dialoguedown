using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// A block conditional: an ordered list of mutually-exclusive <see cref="Branches"/> — the
/// <c>if</c>, its <c>elseif</c>s, and an optional <c>else</c>. At play time the first branch whose
/// condition holds is taken; otherwise the optional <c>else</c> is taken. Without a matching condition or
/// <c>else</c>, no branch is taken. It mirrors <see cref="Choices"/> — a group that holds arms,
/// each owning a body.
/// </summary>
internal sealed record ControlBlock(IReadOnlyList<Branch> Branches, SourceSpan Span)
    : ScriptBlock(Span);

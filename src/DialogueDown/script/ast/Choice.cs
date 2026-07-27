using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// One selectable option at a branch. It holds its own <see cref="Body"/> blocks, so a
/// choice can carry a <see cref="Line"/> and a nested <see cref="Choices"/> — which is
/// how nested choices are represented. A choice is not itself a <see cref="ScriptBlock"/>.
/// An optional <see cref="Condition"/> guards the whole option: a conditional option is offered
/// only when the condition is true.
/// </summary>
internal sealed record Choice(
    IReadOnlyList<ScriptBlock> Body, SourceSpan Span, Condition? Condition = null) : ScriptNode(Span)
{
    /// <summary>Whether this option is guarded by a condition.</summary>
    public bool IsConditional => Condition is not null;
}

using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// A group of options offered at once: the shared base of the player-facing
/// <see cref="Choices"/> and the engine-resolved <see cref="RandomChoices"/>, so a pass that
/// treats both alike (such as the choice-nesting depth check) can query one type.
/// </summary>
internal abstract record ChoiceGroup(SourceSpan Span) : ScriptBlock(Span);

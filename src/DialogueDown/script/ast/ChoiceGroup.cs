using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// A branch that offers several options at once — the shared base of the player-facing
/// <see cref="Choices"/> and the engine-resolved <see cref="RandomChoices"/>. It exists so a
/// pass that cares about any branch group (such as the choice-nesting depth check) can query
/// one type; the two remain distinct records, since a player choice and a random choice are
/// built, resolved, and rendered differently.
/// </summary>
internal abstract record ChoiceGroup(SourceSpan Span) : ScriptBlock(Span);

using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// A group of weighted options the engine resolves to exactly one at runtime, by weight — it
/// shows no player menu. Its items are <see cref="RandomOption"/>s, not <see cref="Choice"/>s,
/// because the player never selects one — the engine does.
/// </summary>
/// <remarks>
/// <code>
/// - `70%` Guard: Halt!
/// - `%` Guard: Who goes there?
/// </code>
/// </remarks>
internal sealed record RandomChoices(
    IReadOnlyList<RandomOption> Options, SourceSpan Span) : ChoiceGroup(Span);

using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// A group of weighted options the engine resolves to exactly one at runtime, by weight — it
/// shows no player menu. Named to parallel <see cref="Choices"/>, the player-facing branch,
/// but kept a distinct block: the two are separate constructs that later stages, the runtime,
/// and the report treat differently. Its items are <see cref="RandomOption"/>s, not
/// <see cref="Choice"/>s, because the player never selects one — the engine does.
/// </summary>
internal sealed record RandomChoices(
    IReadOnlyList<RandomOption> Options, SourceSpan Span) : ChoiceGroup(Span);

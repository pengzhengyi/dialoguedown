using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// One option in a <see cref="RandomChoices"/> group: its <see cref="Weight"/> and the
/// <see cref="Body"/> blocks that run if the engine selects it. It is an <em>option</em>, not
/// a <see cref="Choice"/>: the player never selects it — the engine does — so it parallels
/// <see cref="Choice"/> structurally while keeping the neutral name. Like a choice, it is a
/// <see cref="ScriptNode"/>, not a <see cref="ScriptBlock"/>.
/// </summary>
internal sealed record RandomOption(
    ChoiceWeight Weight, IReadOnlyList<ScriptBlock> Body, SourceSpan Span) : ScriptNode(Span);

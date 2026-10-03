using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// One option in a <see cref="RandomChoices"/> group: its <see cref="Weight"/> and the
/// <see cref="Body"/> blocks that run if the engine selects it, guarded by an optional
/// <see cref="Condition"/>. Like a <see cref="Choice"/>, it is a <see cref="ScriptNode"/>, not a
/// <see cref="ScriptBlock"/>.
/// </summary>
internal sealed record RandomOption(
    ChoiceWeight Weight, IReadOnlyList<ScriptBlock> Body, SourceSpan Span,
    Condition? Condition = null) : ScriptNode(Span), IConditional;

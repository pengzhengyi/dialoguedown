using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// The weight on a <see cref="RandomOption"/> in a <see cref="RandomChoices"/> group: how likely
/// the engine is to select that option, relative to its siblings. A closed set — a concrete
/// <see cref="NumberWeight"/> percentage, an <see cref="AutoWeight"/> that claims an equal share
/// of the leftover, or a <see cref="QueryWeight"/> the runtime computes from game state — so a
/// consumer can handle every case exhaustively. Every weight is a spanned <see cref="ScriptNode"/>,
/// so diagnostics and tooling can point at the exact weight code span.
/// </summary>
internal abstract record ChoiceWeight(SourceSpan Span) : ScriptNode(Span);

using DialogueDown.Common;

namespace DialogueDown.Script.Ast;

/// <summary>
/// A bare <c>`%`</c> weight that claims an equal share of the percentage the explicit
/// weights leave. All auto weights carry the same meaning; only their source span differs.
/// </summary>
internal sealed record AutoWeight(SourceSpan Span) : ChoiceWeight(Span);

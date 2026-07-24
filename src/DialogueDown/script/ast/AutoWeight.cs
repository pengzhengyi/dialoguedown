namespace DialogueDown.Script.Ast;

/// <summary>
/// A bare <c>`%`</c> weight that claims an equal share of the percentage the explicit
/// weights leave. All auto weights are interchangeable, so they carry no data.
/// </summary>
internal sealed record AutoWeight : ChoiceWeight;

namespace DialogueDown.Script.Ast;

/// <summary>A concrete percentage weight, such as <c>`50%`</c>. Weights are relative, so
/// the value is normalized against its siblings rather than required to total 100.</summary>
internal sealed record NumberWeight(double Percentage) : ChoiceWeight;

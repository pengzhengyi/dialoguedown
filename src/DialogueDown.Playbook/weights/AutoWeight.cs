namespace DialogueDown.Playbook.Weights;

/// <summary>
/// An unweighted option, sharing evenly in whatever the weighted options leave.
/// </summary>
/// <remarks>It carries nothing: the kind is the whole weight.</remarks>
public sealed record AutoWeight : ChoiceWeight;

using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Weights;

/// <summary>
/// Turns a random choice's number weights into selection probabilities. Only number weights
/// (<see cref="NumberWeight"/> and <see cref="AutoWeight"/>) are resolvable here; a dynamic,
/// game-state weight is resolved by the runtime.
/// </summary>
internal interface IWeightNormalization
{
    /// <summary>Resolves and normalizes <paramref name="weights"/> in order.</summary>
    WeightDistribution Normalize(IReadOnlyList<ChoiceWeight> weights);
}

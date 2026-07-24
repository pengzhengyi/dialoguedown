using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Weights;

/// <summary>
/// Turns a random choice's static weights into selection probabilities. A seam so the
/// arithmetic is tested in isolation and shared by the compile-time weight check and, later,
/// the runtime. Only static weights (<see cref="NumberWeight"/> and <see cref="AutoWeight"/>)
/// are resolvable here; a dynamic, game-state weight is resolved by the runtime and is out of
/// scope.
/// </summary>
internal interface IWeightNormalization
{
    /// <summary>Resolves and normalizes <paramref name="weights"/> in order.</summary>
    WeightDistribution Normalize(IReadOnlyList<ChoiceWeight> weights);
}

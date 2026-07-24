using DialogueDown.Script.Weights;

namespace DialogueDown.Script.Validation;

/// <summary>
/// Creates a structural validator with DialogueDown's built-in rules. Both the container-free
/// compiler factory and the dependency-injection registration call this method, so compilers
/// created through either composition root run the same structural rule set.
/// </summary>
internal static class StructuralValidatorFactory
{
    public static IStructuralValidator CreateDefault() =>
        new StructuralValidator(
        [
            new MultipleJumpsOnLineRule(),
            new ChoiceNestingDepthRule(),
            new WeightTotalRule(new DefaultWeightNormalization()),
            new SingleOptionRandomChoiceRule(),
        ]);
}

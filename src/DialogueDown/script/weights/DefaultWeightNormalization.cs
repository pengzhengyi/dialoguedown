using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Weights;

/// <summary>
/// The default weight-normalization strategy (Shape A): explicit percentages are taken as
/// written, each auto weight claims an equal share of the percentage they leave, and every
/// resolved weight is divided by their total so the probabilities sum to 1. A zero weight
/// total is reported as an error by validation (`DLG2010`); the normalizer still recovers to
/// a uniform distribution, so no consumer ever divides by zero.
/// </summary>
internal sealed class DefaultWeightNormalization : IWeightNormalization
{
    private const double OneHundredPercent = 100;

    public WeightDistribution Normalize(IReadOnlyList<ChoiceWeight> weights)
    {
        RequireNonNegativePercentages(weights);

        if (weights.Count == 0)
        {
            return new WeightDistribution([], 0);
        }

        var explicitSum = weights.OfType<NumberWeight>().Sum(number => number.Percentage);
        var autoCount = weights.OfType<AutoWeight>().Count();
        var autoShare = autoCount > 0 ? Math.Max(0, OneHundredPercent - explicitSum) / autoCount : 0;

        var resolved = weights.Select(weight => Resolve(weight, autoShare)).ToList();
        var rawTotal = resolved.Sum();

        // A zero total is a validation error (DLG2010); recover to a uniform distribution so
        // the graph and runtime never divide by zero on a still-collected, best-effort result.
        var probabilities = rawTotal > 0
            ? resolved.Select(value => value / rawTotal).ToList()
            : Enumerable.Repeat(1.0 / weights.Count, weights.Count).ToList();

        return new WeightDistribution(probabilities, rawTotal);
    }

    // A negative percentage cannot mean a probability; recognition rejects it as a diagnostic,
    // so reaching here is a caller bug. Failing fast keeps the output probabilities valid —
    // non-negative and summing to 1 — rather than silently normalizing nonsense.
    private static void RequireNonNegativePercentages(IReadOnlyList<ChoiceWeight> weights)
    {
        foreach (var number in weights.OfType<NumberWeight>())
        {
            if (number.Percentage < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(weights), number.Percentage,
                    "A choice weight percentage must be non-negative; reject negative weights "
                    + "before normalizing.");
            }
        }
    }

    private static double Resolve(ChoiceWeight weight, double autoShare) => weight switch
    {
        NumberWeight number => number.Percentage,
        AutoWeight => autoShare,
        _ => throw new ArgumentOutOfRangeException(
            nameof(weight), weight.GetType(), "Unhandled choice weight kind."),
    };
}

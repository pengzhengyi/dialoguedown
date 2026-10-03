using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Weights;

/// <summary>
/// The default weight-normalization strategy: explicit percentages are taken as written, each
/// auto weight claims an equal share of the percentage they leave, and every resolved weight is
/// divided by their total so the probabilities sum to 1. A zero weight total is reported as an
/// error by validation; the normalizer still recovers to a uniform distribution, so no consumer
/// ever divides by zero.
/// </summary>
/// <remarks>
/// <c>`50%`</c>, <c>`%`</c>, <c>`%`</c> resolve to 50, 25, and 25, giving 0.5, 0.25, and 0.25.
/// <c>`30%`</c>, <c>`30%`</c> total 60 and still give 0.5 and 0.5.
/// </remarks>
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

        var probabilities = rawTotal > 0
            ? resolved.Select(value => value / rawTotal).ToList()
            : Enumerable.Repeat(1.0 / weights.Count, weights.Count).ToList();

        return new WeightDistribution(probabilities, rawTotal);
    }

    // A negative percentage cannot mean a probability, and recognition reports one instead of
    // building it, so one here is a caller bug.
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
        QueryWeight => throw new ArgumentOutOfRangeException(
            nameof(weight), weight.GetType(),
            "A query weight has no compile-time value; resolve it at runtime before normalizing."),
        _ => throw new ArgumentOutOfRangeException(
            nameof(weight), weight.GetType(), "Unhandled choice weight kind."),
    };
}

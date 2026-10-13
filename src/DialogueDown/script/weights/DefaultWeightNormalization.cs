using DialogueDown.Script.Ast;
using DrawingWeight = DialogueDown.Playbook.Weights.DrawingWeight;

namespace DialogueDown.Script.Weights;

/// <summary>
/// The default weight-normalization strategy: each weight becomes a <see cref="DrawingWeight"/>,
/// whose shares are the same arithmetic a runner draws by, and every share is divided by their
/// total so the probabilities sum to 1. A zero weight total is reported as an error by
/// validation; the normalizer still recovers to a uniform distribution, so no consumer ever
/// divides by zero.
/// </summary>
/// <remarks>
/// <c>`50%`</c>, <c>`%`</c>, <c>`%`</c> resolve to 50, 25, and 25, giving 0.5, 0.25, and 0.25.
/// <c>`30%`</c>, <c>`30%`</c> total 60 and still give 0.5 and 0.5.
/// </remarks>
internal sealed class DefaultWeightNormalization : IWeightNormalization
{
    public WeightDistribution Normalize(IReadOnlyList<ChoiceWeight> weights)
    {
        ArgumentNullException.ThrowIfNull(weights);

        if (weights.Count == 0)
        {
            return new WeightDistribution([], 0);
        }

        var shares = DrawingWeight.Shares(weights.Select(ToDrawingWeight));
        var rawTotal = DrawingWeight.Total(shares);

        var probabilities = rawTotal > 0
            ? shares.Select(share => share / rawTotal).ToList()
            : Enumerable.Repeat(1.0 / shares.Length, shares.Length).ToList();

        return new WeightDistribution(probabilities, rawTotal);
    }

    // A negative or an infinite number is refused by the drawing weight; a query weight is refused
    // here, because it has no compile-time value.
    private static DrawingWeight ToDrawingWeight(ChoiceWeight weight) => weight switch
    {
        NumberWeight number => new DrawingWeight.Number(number.Percentage),
        AutoWeight => new DrawingWeight.Auto(),
        QueryWeight => throw new ArgumentOutOfRangeException(
            nameof(weight), weight.GetType(),
            "A query weight has no compile-time value; resolve it at runtime before normalizing."),
        _ => throw new ArgumentOutOfRangeException(
            nameof(weight), weight.GetType(), "Unhandled choice weight kind."),
    };
}

using DialogueDown.Script.Ast;
using PlaybookWeights = DialogueDown.Playbook.Weights;

namespace DialogueDown.Script.Weights;

/// <summary>
/// The default weight-normalization strategy: each weight's share is worked out by the
/// playbook's <see cref="PlaybookWeights.WeightShares"/>, the same arithmetic a runner draws by,
/// and every share is divided by their total so the probabilities sum to 1. A zero weight total is
/// reported as an error by validation; the normalizer still recovers to a uniform distribution, so
/// no consumer ever divides by zero.
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

        var shares = PlaybookWeights.WeightShares.Of(weights.Select(ToPlaybookWeight));
        var rawTotal = PlaybookWeights.WeightShares.Total(shares);

        var probabilities = rawTotal > 0
            ? shares.Select(share => share / rawTotal).ToList()
            : Enumerable.Repeat(1.0 / shares.Length, shares.Length).ToList();

        return new WeightDistribution(probabilities, rawTotal);
    }

    // The shares are worked out over the playbook's weights. A negative or an infinite number is
    // refused there; a query weight is refused here, because it has no compile-time value.
    private static PlaybookWeights.ChoiceWeight ToPlaybookWeight(ChoiceWeight weight) => weight switch
    {
        NumberWeight number => new PlaybookWeights.NumberWeight(number.Percentage),
        AutoWeight => new PlaybookWeights.AutoWeight(),
        QueryWeight => throw new ArgumentOutOfRangeException(
            nameof(weight), weight.GetType(),
            "A query weight has no compile-time value; resolve it at runtime before normalizing."),
        _ => throw new ArgumentOutOfRangeException(
            nameof(weight), weight.GetType(), "Unhandled choice weight kind."),
    };
}

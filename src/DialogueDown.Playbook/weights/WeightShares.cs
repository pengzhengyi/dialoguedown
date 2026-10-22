using System.Collections.Immutable;
using DialogueDown.Playbook.Edges;

namespace DialogueDown.Playbook.Weights;

/// <summary>
/// Works out the shares of a random choice's random options: each becomes a
/// <see cref="DrawingWeight"/>, which does the arithmetic. One in play becomes its weight, a key
/// weight becoming the number the world answered, and an excluded one becomes a number of zero.
/// </summary>
public static class WeightShares
{
    /// <summary>The share each random option comes to.</summary>
    /// <param name="randomOptions">
    /// The random options, in the order written. Each one in play is weighted by a
    /// <see cref="NumberWeight"/>, an <see cref="AutoWeight"/>, or a <see cref="QueryWeight"/>
    /// whose key <paramref name="keyWeights"/> answers.
    /// </param>
    /// <param name="arbiter">Decides which random options are excluded.</param>
    /// <param name="keyWeights">The numbers the world answered for the key weights.</param>
    /// <returns>One share per random option, in the same order.</returns>
    /// <remarks>
    /// The random options in play share the draw as if the excluded ones were not written, so an
    /// auto weight takes an equal part of what the numbers in play leave of 100. An excluded random
    /// option has a share of zero, and its weight is never read. With <c>`50%`</c> excluded beside
    /// <c>`30%`</c> and <c>`%`</c>, the shares come to 0, 30, and 70.
    /// <para>
    /// A key weight in play counts as the number the world answered for it, so
    /// <c>`Guard.Mood%`</c> answered <c>20</c> beside <c>`%`</c> comes to 20 and 80.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A number in play, written or answered, is negative or not finite.
    /// </exception>
    public static ImmutableArray<double> Of(
        IEnumerable<RandomOptionEdge> randomOptions, IRandomOptionArbiter arbiter, IKeyWeightAnswers keyWeights)
    {
        ArgumentNullException.ThrowIfNull(randomOptions);
        ArgumentNullException.ThrowIfNull(arbiter);
        ArgumentNullException.ThrowIfNull(keyWeights);

        return DrawingWeight.Shares(randomOptions.Select(option => ToDrawingWeight(option, arbiter, keyWeights)));
    }

    /// <summary>Adds the shares up, one at a time from the first.</summary>
    /// <param name="shares">The shares, in the order written.</param>
    /// <returns>
    /// Their sum: zero for no shares, and infinite when the shares are too large to add up.
    /// </returns>
    public static double Total(IEnumerable<double> shares) => DrawingWeight.Total(shares);

    // An excluded random option counts as a number of zero, which adds nothing to the numbers and
    // takes no part of what they leave.
    private static DrawingWeight ToDrawingWeight(
        RandomOptionEdge option, IRandomOptionArbiter arbiter, IKeyWeightAnswers keyWeights) =>
        option switch
        {
            null => throw new ArgumentNullException(nameof(option), "A random choice has no missing random options."),
            _ when arbiter.Excludes(option) => new DrawingWeight.Number(0),
            _ => ToDrawingWeight(option.Weight, keyWeights),
        };

    private static DrawingWeight ToDrawingWeight(ChoiceWeight weight, IKeyWeightAnswers keyWeights) => weight switch
    {
        NumberWeight number => new DrawingWeight.Number(number.Percentage),
        AutoWeight => new DrawingWeight.Auto(),
        QueryWeight { Key: var key } => new DrawingWeight.Number(keyWeights.NumberFor(key)),
        _ => throw new NotSupportedException($"No drawing weight is defined for {weight.GetType().Name}."),
    };
}

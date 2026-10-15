using System.Collections.Immutable;
using DialogueDown.Playbook.Edges;

namespace DialogueDown.Playbook.Weights;

/// <summary>
/// Works out the shares of a random choice's random options: each becomes a
/// <see cref="DrawingWeight"/>, which does the arithmetic. One in play becomes its weight, and an
/// excluded one a number of zero.
/// </summary>
public static class WeightShares
{
    /// <summary>The share each random option comes to.</summary>
    /// <param name="randomOptions">
    /// The random options, in the order written. Each one in play is weighted by a
    /// <see cref="NumberWeight"/> or an <see cref="AutoWeight"/>.
    /// </param>
    /// <param name="arbiter">Decides which random options are excluded.</param>
    /// <returns>One share per random option, in the same order.</returns>
    /// <remarks>
    /// The random options in play share the draw as if the excluded ones were not written, so an
    /// auto weight takes an equal part of what the numbers in play leave of 100. An excluded random
    /// option has a share of zero, and its weight is never read. With <c>`50%`</c> excluded beside
    /// <c>`30%`</c> and <c>`%`</c>, the shares come to 0, 30, and 70.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// A random option in play is weighted by a <see cref="QueryWeight"/>, whose number only the
    /// world knows.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A number in play is negative or not finite, which no share can be.
    /// </exception>
    public static ImmutableArray<double> Of(IEnumerable<RandomOptionEdge> randomOptions, IRandomOptionArbiter arbiter)
    {
        ArgumentNullException.ThrowIfNull(randomOptions);
        ArgumentNullException.ThrowIfNull(arbiter);

        return DrawingWeight.Shares(randomOptions.Select(option => ToDrawingWeight(option, arbiter)));
    }

    /// <summary>Adds the shares up, one at a time from the first.</summary>
    /// <param name="shares">The shares, in the order written.</param>
    /// <returns>
    /// Their sum: zero for no shares, and infinite when the shares are too large to add up.
    /// </returns>
    public static double Total(IEnumerable<double> shares) => DrawingWeight.Total(shares);

    // An excluded random option weighs nothing: a number of zero adds nothing to the numbers, and
    // takes no part of what they leave.
    private static DrawingWeight ToDrawingWeight(RandomOptionEdge option, IRandomOptionArbiter arbiter) => option switch
    {
        null => throw new ArgumentNullException(nameof(option), "A random choice has no missing random options."),
        _ when arbiter.Excludes(option) => new DrawingWeight.Number(0),
        _ => ToDrawingWeight(option.Weight),
    };

    private static DrawingWeight ToDrawingWeight(ChoiceWeight weight) => weight switch
    {
        NumberWeight number => new DrawingWeight.Number(number.Percentage),
        AutoWeight => new DrawingWeight.Auto(),
        QueryWeight { Key: var key } => throw new ArgumentException(
            $"The key weight '{key}' has no share until the world answers it.", nameof(weight)),
        _ => throw new NotSupportedException($"No drawing weight is defined for {weight.GetType().Name}."),
    };
}

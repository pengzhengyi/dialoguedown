using System.Collections.Immutable;
using DialogueDown.Playbook.Edges;

namespace DialogueDown.Playbook.Weights;

/// <summary>
/// Works out the shares of a random choice's random options: each one's weight becomes a
/// <see cref="DrawingWeight"/>, which does the arithmetic.
/// </summary>
public static class WeightShares
{
    /// <summary>The share each random option comes to.</summary>
    /// <param name="randomOptions">
    /// The random options, in the order written, each weighted by a <see cref="NumberWeight"/> or
    /// an <see cref="AutoWeight"/>.
    /// </param>
    /// <returns>One share per random option, in the same order.</returns>
    /// <exception cref="ArgumentException">
    /// A random option is weighted by a <see cref="QueryWeight"/>, whose number only the world
    /// knows.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A number is negative or not finite, which no share can be.
    /// </exception>
    public static ImmutableArray<double> Of(IEnumerable<RandomOptionEdge> randomOptions)
    {
        ArgumentNullException.ThrowIfNull(randomOptions);

        return DrawingWeight.Shares(randomOptions.Select(ToDrawingWeight));
    }

    /// <summary>Adds the shares up, one at a time from the first.</summary>
    /// <param name="shares">The shares, in the order written.</param>
    /// <returns>
    /// Their sum: zero for no shares, and infinite when the shares are too large to add up.
    /// </returns>
    public static double Total(IEnumerable<double> shares) => DrawingWeight.Total(shares);

    private static DrawingWeight ToDrawingWeight(RandomOptionEdge option) =>
        option is null
            ? throw new ArgumentNullException(nameof(option), "A random choice has no missing random options.")
            : ToDrawingWeight(option.Weight);

    private static DrawingWeight ToDrawingWeight(ChoiceWeight weight) => weight switch
    {
        NumberWeight number => new DrawingWeight.Number(number.Percentage),
        AutoWeight => new DrawingWeight.Auto(),
        QueryWeight { Key: var key } => throw new ArgumentException(
            $"The key weight '{key}' has no share until the world answers it.", nameof(weight)),
        _ => throw new NotSupportedException($"No drawing weight is defined for {weight.GetType().Name}."),
    };
}

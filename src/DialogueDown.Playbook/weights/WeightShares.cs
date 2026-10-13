using System.Collections.Immutable;

namespace DialogueDown.Playbook.Weights;

/// <summary>
/// Works out the shares of a playbook's weights: each weight becomes a
/// <see cref="DrawingWeight"/>, which does the arithmetic.
/// </summary>
public static class WeightShares
{
    /// <summary>The share each of a playbook's weights comes to.</summary>
    /// <param name="weights">
    /// Each a <see cref="NumberWeight"/> or an <see cref="AutoWeight"/>, in the order written.
    /// </param>
    /// <returns>One share per weight, in the same order.</returns>
    /// <exception cref="ArgumentException">
    /// A weight is a <see cref="QueryWeight"/>, whose number only the world knows.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A number is negative or not finite, which no share can be.
    /// </exception>
    public static ImmutableArray<double> Of(IEnumerable<ChoiceWeight> weights)
    {
        ArgumentNullException.ThrowIfNull(weights);

        return DrawingWeight.Shares(weights.Select(ToDrawingWeight));
    }

    /// <summary>Adds the shares up, one at a time from the first.</summary>
    /// <param name="shares">The shares, in the order written.</param>
    /// <returns>
    /// Their sum: zero for no shares, and infinite when the shares are too large to add up.
    /// </returns>
    public static double Total(IEnumerable<double> shares) => DrawingWeight.Total(shares);

    private static DrawingWeight ToDrawingWeight(ChoiceWeight weight) => weight switch
    {
        NumberWeight number => new DrawingWeight.Number(number.Percentage),
        AutoWeight => new DrawingWeight.Auto(),
        QueryWeight { Key: var key } => throw new ArgumentException(
            $"The key weight '{key}' has no share until the world answers it.", nameof(weight)),
        null => throw new ArgumentNullException(nameof(weight), "A random choice has no missing weights."),
        _ => throw new NotSupportedException($"No drawing weight is defined for {weight.GetType().Name}."),
    };
}

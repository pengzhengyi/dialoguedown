using System.Collections.Immutable;
using System.Globalization;

namespace DialogueDown.Playbook.Weights;

/// <summary>
/// A weight ready for its share to be worked out: a number, or an auto weight.
/// </summary>
/// <remarks>
/// A number is its own share, and each auto weight takes an equal part of what the numbers leave
/// of 100. <c>`50%`</c>, <c>`%`</c>, <c>`%`</c> come to 50, 25, and 25. Numbers that reach 100
/// leave nothing, so <c>`70%`</c>, <c>`50%`</c>, <c>`%`</c> come to 70, 50, and 0. Shares are
/// relative: a draw is made against their total, whatever it is.
/// <para>
/// A key weight has no number until the world answers it, so there is no drawing weight for a
/// key: once answered, it is a number. A random option left out of the draw weighs nothing, which
/// is a number of zero: it adds nothing to the numbers, and it is not an auto weight, so it takes
/// no part of the rest.
/// </para>
/// </remarks>
public abstract record DrawingWeight
{
    private const double OneHundredPercent = 100;

    // Private, so the two kinds nested here are the only ones there can be.
    private DrawingWeight()
    {
    }

    /// <summary>The share each weight comes to.</summary>
    /// <param name="weights">The weights, in the order written.</param>
    /// <returns>One share per weight, in the same order.</returns>
    public static ImmutableArray<double> Shares(IEnumerable<DrawingWeight> weights)
    {
        ArgumentNullException.ThrowIfNull(weights);

        // An auto weight takes part of what every number leaves, including numbers written after
        // it, so the numbers are added up before any auto weight's share is known.
        var drawing = weights.ToList();
        var leftover = Leftover(drawing);
        var autoShareCount = AutoShareCount(drawing);
        var autoShareAmount = leftover / autoShareCount;

        return [.. drawing.Select(weight => weight switch
        {
            Number number => number.Percentage,
            Auto => autoShareAmount,
            null => throw new ArgumentNullException(nameof(weights), "A random choice has no missing weights."),
            _ => throw new NotSupportedException($"No share is defined for {weight.GetType().Name}."),
        })];
    }

    /// <summary>Adds the shares up, one at a time from the first.</summary>
    /// <param name="shares">The shares, in the order written.</param>
    /// <returns>
    /// Their sum: zero for no shares, and infinite when the shares are too large to add up.
    /// </returns>
    /// <remarks>
    /// Floating-point addition depends on order, and adding from the first is what lets two
    /// runtimes reach the same total for the same shares.
    /// </remarks>
    public static double Total(IEnumerable<double> shares)
    {
        ArgumentNullException.ThrowIfNull(shares);

        var total = 0.0;
        foreach (var share in shares)
        {
            total += share;
        }

        return total;
    }

    /// <summary>Whether a number can be a weight: finite and not negative.</summary>
    /// <param name="percentage">The number.</param>
    /// <returns><see langword="true"/> when the number can be a weight.</returns>
    public static bool IsNonNegativeAndFinite(double percentage) =>
        double.IsFinite(percentage) && percentage >= 0;

    // What the numbers leave of 100, added up in the order written.
    private static double Leftover(IEnumerable<DrawingWeight> weights) =>
        Math.Max(0, OneHundredPercent - Total(weights.OfType<Number>().Select(number => number.Percentage)));

    private static int AutoShareCount(IEnumerable<DrawingWeight> weights) => weights.Count(weight => weight is Auto);

    private static double AssertNonNegativeAndFinite(double percentage) =>
        IsNonNegativeAndFinite(percentage)
            ? percentage
            : throw new ArgumentOutOfRangeException(
                nameof(percentage),
                percentage,
                $"A number weight of {percentage.ToString(CultureInfo.InvariantCulture)} has no share: "
                    + "a weight must be finite and not negative.");

    /// <summary>A number, which is its own share.</summary>
    /// <param name="Percentage">The percentage, as written or as the world answered it.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The percentage is negative or not finite, which no share can be.
    /// </exception>
    public sealed record Number(double Percentage) : DrawingWeight
    {
        /// <summary>Gets the percentage, as written or as the world answered it.</summary>
        public double Percentage { get; } = AssertNonNegativeAndFinite(Percentage);
    }

    /// <summary>An auto weight, which takes an equal part of what the numbers leave of 100.</summary>
    public sealed record Auto : DrawingWeight;
}

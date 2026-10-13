using System.Globalization;

namespace DialogueDown.Playbook.Weights;

/// <summary>
/// A weight ready for its share to be worked out: a number, or an auto weight.
/// </summary>
/// <remarks>
/// A key weight has no number until the world answers it, so there is no resolved weight for a
/// key: once answered, it is a number. An arm left out of the draw weighs nothing, which is a
/// number of zero: it adds nothing to the numbers, and it is not an auto weight, so it takes no
/// part of the rest.
/// </remarks>
public abstract record ResolvedWeight
{
    // Private, so the two kinds nested here are the only ones there can be.
    private ResolvedWeight()
    {
    }

    private static double AssertNonNegativeAndFinite(double percentage) =>
        double.IsFinite(percentage) && percentage >= 0
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
    public sealed record Number(double Percentage) : ResolvedWeight
    {
        /// <summary>Gets the percentage, as written or as the world answered it.</summary>
        public double Percentage { get; } = AssertNonNegativeAndFinite(Percentage);
    }

    /// <summary>An auto weight, which takes an equal part of what the numbers leave of 100.</summary>
    public sealed record Auto : ResolvedWeight;
}

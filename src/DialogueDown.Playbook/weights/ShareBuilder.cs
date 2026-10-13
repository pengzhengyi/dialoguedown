using System.Collections.Immutable;
using System.Globalization;

namespace DialogueDown.Playbook.Weights;

/// <summary>
/// Gathers a random choice's weights one at a time, and works out each one's share once every
/// weight is taken.
/// </summary>
/// <remarks>
/// A number's share is known as soon as it is taken. An auto weight's is not, because it takes an
/// equal part of what every number leaves of 100, including numbers taken after it:
/// <c>`%`</c>, <c>`50%`</c> come to 50 and 50. So an auto weight holds an empty place until
/// <see cref="Freeze"/> fills it in.
/// </remarks>
internal sealed class ShareBuilder
{
    private const double OneHundredPercent = 100;

    // A number's share, or null for an auto weight, whose share Freeze works out.
    private readonly List<double?> _shares = [];

    // The numbers taken so far, added one at a time from the first.
    private double Numbers => Total(_shares.OfType<double>());

    private int AutoShareCount => _shares.Count(share => share is null);

    /// <summary>Creates a builder that has taken all of the given weights.</summary>
    /// <param name="weights">The weights to take, in the order written.</param>
    /// <returns>A builder that has taken all of them.</returns>
    public static ShareBuilder Of(IEnumerable<ChoiceWeight> weights)
    {
        ArgumentNullException.ThrowIfNull(weights);

        var builder = new ShareBuilder();
        foreach (var weight in weights)
        {
            builder.Take(weight);
        }

        return builder;
    }

    /// <summary>Adds the shares up, one at a time from the first.</summary>
    /// <param name="shares">The shares, in the order written.</param>
    /// <returns>
    /// Their sum: zero for no shares, and infinite when the shares are too large to add up.
    /// </returns>
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

    /// <summary>Takes the next weight, in the order written.</summary>
    /// <param name="weight">A <see cref="NumberWeight"/> or an <see cref="AutoWeight"/>.</param>
    /// <exception cref="ArgumentException">
    /// The weight is a <see cref="QueryWeight"/>, whose number only the world knows.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The weight is a number that is negative or not finite, which no share can be.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// The weight is of a kind this builder does not handle.
    /// </exception>
    public void Take(ChoiceWeight weight)
    {
        ArgumentNullException.ThrowIfNull(weight);

        switch (weight)
        {
            case NumberWeight { Percentage: var percentage }:
                _shares.Add(AssertNonNegativeAndFinite(percentage, nameof(weight)));
                break;
            case AutoWeight:
                _shares.Add(null);
                break;
            case QueryWeight { Key: var key }:
                throw new ArgumentException(
                    $"The key weight '{key}' has no share until the world answers it; replace it "
                        + "with a number weight holding the answer.",
                    nameof(weight));
            default:
                throw new NotSupportedException($"No share is defined for {weight.GetType().Name}.");
        }
    }

    /// <summary>Works out the share of every weight taken so far.</summary>
    /// <returns>One share per weight taken, in the order taken.</returns>
    public ImmutableArray<double> Freeze()
    {
        // Counted once, rather than again for every empty place filled.
        var autoShareCount = AutoShareCount;
        var leftover = Math.Max(0, OneHundredPercent - Numbers);

        return [.. _shares.Select(share => share ?? leftover / autoShareCount)];
    }

    private static double AssertNonNegativeAndFinite(double percentage, string paramName) =>
        double.IsFinite(percentage) && percentage >= 0
            ? percentage
            : throw new ArgumentOutOfRangeException(
                paramName,
                percentage,
                $"A number weight of {percentage.ToString(CultureInfo.InvariantCulture)} has no share: "
                    + "a weight must be finite and not negative.");
}

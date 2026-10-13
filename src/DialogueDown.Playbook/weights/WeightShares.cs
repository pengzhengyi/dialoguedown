using System.Collections.Immutable;

namespace DialogueDown.Playbook.Weights;

/// <summary>
/// Works out each weight's share of a random choice, and adds the shares up.
/// </summary>
/// <remarks>
/// A number is its own share, and each auto weight takes an equal part of what the numbers leave
/// of 100. <c>`50%`</c>, <c>`%`</c>, <c>`%`</c> come to 50, 25, and 25. Numbers that reach 100
/// leave nothing, so <c>`70%`</c>, <c>`50%`</c>, <c>`%`</c> come to 70, 50, and 0.
/// <para>
/// Shares are relative: a draw is made against their total, whatever it is. Every addition is
/// made one at a time from the first weight, because floating-point addition depends on order,
/// and the same order is what lets two runtimes reach the same total for the same weights.
/// </para>
/// </remarks>
public static class WeightShares
{
    /// <summary>The share each weight comes to.</summary>
    /// <param name="weights">
    /// Each a <see cref="NumberWeight"/> or an <see cref="AutoWeight"/>, in the order written. A
    /// key weight has a share only once the world answers it, so it is replaced by a number weight
    /// holding the answer first.
    /// </param>
    /// <returns>One share per weight, in the same order.</returns>
    /// <exception cref="ArgumentException">A weight is a <see cref="QueryWeight"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A number is negative or not finite, which no share can be.
    /// </exception>
    public static ImmutableArray<double> Of(IEnumerable<ChoiceWeight> weights) =>
        ShareBuilder.Of(weights).Freeze();

    /// <summary>Adds the shares up, one at a time from the first.</summary>
    /// <param name="shares">The shares, in the order written.</param>
    /// <returns>
    /// Their sum: zero for no shares, and infinite when the shares are too large to add up.
    /// </returns>
    public static double Total(IEnumerable<double> shares) => ShareBuilder.Total(shares);
}

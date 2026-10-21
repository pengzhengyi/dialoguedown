using System.Collections.Immutable;
using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Weights;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Stepping;

/// <summary>
/// Works out a random choice's shares, which of its random options are excluded, and whether any
/// of them is drawable.
/// </summary>
/// <remarks>
/// A random option is drawable when its share is above zero, so a random choice with nothing
/// drawable has a total of zero. The compiler reports a zero total it can see in the weights
/// written. At runtime the total can also be zero by what the world says: it answers <c>0</c> for
/// <c>`Guard.Mood%`</c> beside <c>`0%`</c>, or withholds the condition of every random option that
/// would otherwise be drawable.
/// <para>
/// A random choice with nothing drawable asks for no draw: it is walked past by its fall-through,
/// or refused as leading nowhere when it has none.
/// </para>
/// </remarks>
internal static class RandomChoiceShares
{
    /// <summary>Whether a random choice has nothing drawable.</summary>
    /// <param name="random">The random choice.</param>
    /// <param name="supply">
    /// What the world said about the random options' conditions and key weights, or
    /// <see langword="null"/> when nothing was asked.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when every random option's share is zero, so the random choice is
    /// walked past rather than drawn at.
    /// </returns>
    /// <remarks>
    /// <see langword="false"/> also when the shares cannot be worked out: a random option is guarded
    /// or has a key weight, and nothing was asked. Such a random choice is not walked past: arrival
    /// asks the world first.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A key weight in play was answered with a number that is negative or not finite.
    /// </exception>
    public static bool HasNothingDrawable(RandomChoiceNode random, Supply? supply = null)
    {
        ArgumentNullException.ThrowIfNull(random);

        return TryWorkOut(random, supply, out var shares) && !AnyDrawable(shares);
    }

    /// <summary>Whether any random option is drawable, given their shares.</summary>
    /// <param name="shares">Every random option's share, in the order written.</param>
    /// <returns><see langword="true"/> when at least one share is above zero.</returns>
    public static bool AnyDrawable(ImmutableArray<double> shares) => shares.Any(share => share > 0);

    /// <summary>Works out each random option's share, which is zero for an excluded one.</summary>
    /// <param name="random">The random choice.</param>
    /// <param name="supply">
    /// What the world said about the random options' conditions and key weights, or
    /// <see langword="null"/> when nothing was asked.
    /// </param>
    /// <param name="shares">
    /// One share per random option, in the order written; unset when the shares cannot be worked
    /// out.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the shares are worked out; <see langword="false"/> when a random
    /// option is guarded or has a key weight, and nothing was asked.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A key weight in play was answered with a number that is negative or not finite.
    /// </exception>
    public static bool TryWorkOut(RandomChoiceNode random, Supply? supply, out ImmutableArray<double> shares)
    {
        ArgumentNullException.ThrowIfNull(random);

        var randomOptions = random.RandomOptions();

        if (CannotBeWorkedOut(randomOptions, supply))
        {
            shares = default;
            return false;
        }

        shares = WeightShares.Of(randomOptions, new GuardArbiter(supply), new AnsweredKeyWeights(supply));
        return true;
    }

    /// <summary>The random options of a random choice, without the excluded ones.</summary>
    /// <param name="random">The random choice.</param>
    /// <param name="supply">What the world said about the random options' conditions.</param>
    /// <returns>The random options in play, in the order written.</returns>
    /// <remarks>
    /// A random option is excluded when the world withholds its guard. Told
    /// <c>{ "Guard.IsAngry": false }</c>, this random choice is left with "Move along." only:
    /// <code>
    /// - `Guard.IsAngry?` `50%` Guard: Halt!
    /// - `%` Guard: Move along.
    /// </code>
    /// </remarks>
    public static ImmutableArray<RandomOptionEdge> WithoutExcluded(RandomChoiceNode random, Supply supply)
    {
        ArgumentNullException.ThrowIfNull(random);
        ArgumentNullException.ThrowIfNull(supply);

        var arbiter = new GuardArbiter(supply);

        return [.. random.RandomOptions().Where(option => !arbiter.Excludes(option))];
    }

    // A guarded random option is in play only when the world allows its condition, and a key weight
    // counts as the number the world answers for it, so either needs the world to have been asked.
    private static bool CannotBeWorkedOut(ImmutableArray<RandomOptionEdge> randomOptions, Supply? supply) =>
        supply is null && randomOptions.Any(option => option.Condition is not null || option.Weight is QueryWeight);
}

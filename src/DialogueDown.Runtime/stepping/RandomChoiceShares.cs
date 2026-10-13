using System.Collections.Immutable;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Weights;

namespace DialogueDown.Runtime.Stepping;

/// <summary>
/// What a random choice's random options' shares come to, and whether a draw could land on any of
/// them.
/// </summary>
/// <remarks>
/// A random option is drawable when its share is above zero. Shares are never negative, so a random
/// choice with nothing drawable is one whose shares add up to zero: the zero total the compiler
/// reports when it can work the shares out from the script alone.
/// <para>
/// A random choice with nothing drawable asks for no draw at all: the run walks past it by its
/// fall-through.
/// </para>
/// </remarks>
internal static class RandomChoiceShares
{
    /// <summary>Whether a random choice has no random option a draw could land on.</summary>
    /// <param name="random">The random choice.</param>
    /// <returns>
    /// <see langword="true"/> when every random option's share is zero, so the random choice is
    /// walked past rather than drawn at.
    /// </returns>
    /// <remarks>
    /// Only number and auto weights have a share before the world is asked. A random choice with a
    /// guarded random option or a key weight is never walked past here, and the runner refuses it
    /// as a node it cannot play.
    /// </remarks>
    public static bool HasNothingDrawable(RandomChoiceNode random)
    {
        ArgumentNullException.ThrowIfNull(random);

        return SharesWithoutAsking(random) is { } shares && !AnyDrawable(shares);
    }

    /// <summary>Whether a draw could land on any of these shares.</summary>
    /// <param name="shares">Every random option's share, in the order written.</param>
    /// <returns><see langword="true"/> when at least one share is above zero.</returns>
    public static bool AnyDrawable(ImmutableArray<double> shares) => shares.Any(share => share > 0);

    // The shares a random choice comes to without asking the world, or none when a random option
    // is guarded or weighted by a key, because only the world can say what those come to.
    private static ImmutableArray<double>? SharesWithoutAsking(RandomChoiceNode random)
    {
        var randomOptions = random.RandomOptions();

        return randomOptions.Any(option => option.Condition is not null || option.Weight is QueryWeight)
            ? null
            : WeightShares.Of(randomOptions.Select(option => option.Weight));
    }
}

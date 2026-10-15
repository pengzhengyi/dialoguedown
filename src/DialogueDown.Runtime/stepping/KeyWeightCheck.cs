using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Weights;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Stepping;

/// <summary>
/// Checks that the world answered each key weight with a valid number.
/// </summary>
/// <remarks>
/// A key weight counts as the number the world answers for it, and a number is valid as a weight
/// when it is finite and not negative, as for a written weight. Asked about <c>Guard.Mood</c> for
/// <c>- `Guard.Mood%` Guard: Lovely day.</c> and told <c>{ "Guard.Mood": 20 }</c>, the run goes
/// on to draw. Told <c>{ "Guard.Mood": -10 }</c>, that random option has no share, and the run
/// refuses the supply as <see cref="RefusalReason.InvalidWeight"/>.
/// <para>
/// Only the random options in play are checked, since an excluded one's weight is never read. A
/// game may calculate a key weight's number from other values, and the result can be negative
/// while a guard excludes its random option:
/// <code>
/// - `Hero.CanAttack?` `Hit.Chance%` Hero: Strike!
/// - `%` Hero: Back away.
/// </code>
/// Told <c>{ "Hero.CanAttack": false, "Hit.Chance": -10 }</c>, the run goes on to draw, and only
/// "Back away." can be drawn.
/// </para>
/// <para>
/// The numbers are checked only once <see cref="AnswerCheck"/> finds every key answered with the
/// kind its use needs, so each key weight has a number to read.
/// </para>
/// </remarks>
internal static class KeyWeightCheck
{
    /// <summary>Whether the world answered a key weight with an invalid number.</summary>
    /// <param name="node">The node that asked.</param>
    /// <param name="supply">What the world said, each key answered with the kind its use needs.</param>
    /// <param name="refusal">
    /// What the run has to say about the numbers, or <see langword="null"/> when each is valid.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when a key weight's number is negative or not finite, in which case
    /// the run refuses the supply.
    /// </returns>
    /// <remarks>
    /// A key read by an excluded random option and by one in play is checked. Keys are listed in
    /// ordinal order, so a log replays exactly.
    /// </remarks>
    public static bool Refuses(Node node, Supply supply, [NotNullWhen(true)] out Refused? refusal)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(supply);

        if (InvalidWeights(node, supply) is { Count: > 0 } invalid)
        {
            refusal = new Refused(
                RefusalReason.InvalidWeight,
                $"The world answered {string.Join(", ", invalid)}, but the number for a key weight must be "
                    + "finite and not negative.");

            return true;
        }

        refusal = null;

        return false;
    }

    private static IReadOnlyList<string> InvalidWeights(Node node, Supply supply) =>
        [.. WeightKeysInPlay(node, supply)
            .Order(StringComparer.Ordinal)
            .Select(key => (Key: key, Number: supply.Number(key)))
            .Where(answer => !DrawingWeight.IsNonNegativeAndFinite(answer.Number))
            .Select(answer => $"{answer.Key} with {answer.Number.ToString(CultureInfo.InvariantCulture)}")];

    private static ImmutableArray<string> WeightKeysInPlay(Node node, Supply supply)
    {
        if (node is not RandomChoiceNode random)
        {
            return [];
        }

        return RandomChoiceShares.WithoutExcluded(random, supply).WeightKeys();
    }
}

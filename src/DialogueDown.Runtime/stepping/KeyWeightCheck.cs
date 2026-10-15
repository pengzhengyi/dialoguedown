using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Weights;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Stepping;

/// <summary>
/// Checks that the world gave each key weight a number a weight can be.
/// </summary>
/// <remarks>
/// A key weight counts as the number the world answers for it, so that number must be one a
/// written weight could be: finite and not negative. Asked about <c>Guard.Mood</c> for
/// <c>- `Guard.Mood%` Guard: Lovely day.</c> and told <c>{ "Guard.Mood": 20 }</c>, the run goes
/// on to draw. Told <c>{ "Guard.Mood": -10 }</c>, it has no share to give that random option, and
/// refuses the supply as <see cref="RefusalReason.InvalidWeight"/>.
/// <para>
/// The numbers are checked only once <see cref="AnswerCheck"/> finds every key answered with the
/// kind its use needs, so each key weight has a number to read.
/// </para>
/// </remarks>
internal static class KeyWeightCheck
{
    /// <summary>Whether the world answered a key weight with a number no weight can be.</summary>
    /// <param name="node">The node that asked.</param>
    /// <param name="supply">What the world said, each key answered with the kind its use needs.</param>
    /// <param name="refusal">
    /// What the run has to say about the numbers, or <see langword="null"/> when each is a weight.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when a key weight's number is negative or not finite, in which case
    /// the run refuses the supply.
    /// </returns>
    /// <remarks>
    /// A key weight on an excluded random option was asked too, so its number is held to the same
    /// rule, though it counts for nothing in the draw. Keys are listed in ordinal order, so a log
    /// replays exactly.
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
        [.. node.FindNumbersForPlaying()
            .Order(StringComparer.Ordinal)
            .Select(key => (Key: key, Number: supply.Number(key)))
            .Where(answer => !DrawingWeight.IsNonNegativeAndFinite(answer.Number))
            .Select(answer => $"{answer.Key} with {answer.Number.ToString(CultureInfo.InvariantCulture)}")];
}

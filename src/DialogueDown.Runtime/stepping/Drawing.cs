using System.Collections.Immutable;
using System.Globalization;
using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Weights;
using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Situations;

namespace DialogueDown.Runtime.Stepping;

/// <summary>
/// What a random choice does: taking the host's draw, and arriving where the random option it picks
/// leads.
/// </summary>
/// <remarks>
/// A draw is a number from 0 to 1. The shares are laid end to end in the order written, and the
/// draw times their total falls inside one random option's span:
/// <code>
/// shares   0      30       20
/// spans   [0, 0) [0, 30) [30, 50)
/// a draw of 0.7 stands at 35 of 50, inside the third span
/// </code>
/// Each span includes its start and not its end, so a draw at the end of one span picks the next
/// random option, and a random option with a share of zero is never picked. A draw of 1 stands at
/// the total and picks the last random option with a share.
/// </remarks>
internal static class Drawing
{
    /// <summary>Takes the host's draw, and arrives where the random option it picks leads.</summary>
    /// <param name="context">What the run needs and never changes.</param>
    /// <param name="waiting">The random choice waiting for the draw, and the shares the host was shown.</param>
    /// <param name="drawn">The host's draw.</param>
    /// <returns>Where the run now stands, and what it has to say.</returns>
    /// <remarks>
    /// A random choice asks the world nothing as it is left, so where the random option leads is
    /// arrived at directly. A draw outside 0 to 1 is refused, and the random choice keeps waiting for another.
    /// </remarks>
    public static StepResult Drawn(PlayContext context, AwaitingDraw waiting, Drawn drawn)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(waiting);
        ArgumentNullException.ThrowIfNull(drawn);

        return context.NodeAt(waiting.Node) switch
        {
            RandomChoiceNode random => Take(context, waiting, random.RandomOptions(), drawn.Value),
            var notARandomChoice => RefuseDrawn(
                waiting,
                $"Node {waiting.Node}, of kind {notARandomChoice.GetType().Name}, is not a random choice, "
                    + "so there is no Drawn to take there."),
        };
    }

    // The node says where each random option leads; the wait says what share each one has. A wait
    // that does not fit its node is refused before the draw is read, because no draw could be taken
    // there.
    private static StepResult Take(
        PlayContext context, AwaitingDraw waiting, ImmutableArray<RandomOptionEdge> randomOptions, double draw)
    {
        if (waiting.Shares.Length != randomOptions.Length)
        {
            return RefuseDrawn(
                waiting,
                $"The random choice at node {waiting.Node} has {randomOptions.Length} random options, but the "
                    + $"draw was asked for {waiting.Shares.Length} shares, so there is no Drawn to take there.");
        }

        if (!waiting.Shares.Any(share => share > 0))
        {
            return RefuseDrawn(
                waiting,
                $"The random choice at node {waiting.Node} has no random option with a share, so there is "
                    + "no Drawn to take there.");
        }

        return draw is >= 0 and <= 1
            ? Arrival.At(context, randomOptions[RandomOptionAt(waiting.Shares, draw)].Target)
            : RefuseInvalidDraw(waiting, draw);
    }

    // The shares are added one at a time from the first, as their total is, so two runtimes pick
    // the same random option for the same draw. Only a draw of 1 passes every span, and it picks the
    // last random option with a share.
    private static int RandomOptionAt(ImmutableArray<double> shares, double draw)
    {
        var point = draw * WeightShares.Total(shares);
        var sum = 0.0;
        var lastWithAShare = -1;

        for (var at = 0; at < shares.Length; at++)
        {
            sum += shares[at];

            if (point < sum)
            {
                return at;
            }

            if (shares[at] > 0)
            {
                lastWithAShare = at;
            }
        }

        return lastWithAShare;
    }

    private static StepResult RefuseInvalidDraw(AwaitingDraw waiting, double draw) =>
        new(
            new PlayState(waiting),
            [
                new Refused(
                    RefusalReason.InvalidDraw,
                    $"The random choice at node {waiting.Node} takes a draw from 0 to 1, not "
                        + $"{draw.ToString(CultureInfo.InvariantCulture)}."),
            ]);

    // A wait at a random choice can be restored against a playbook whose node there is something
    // else, or whose random options are not the ones the draw was asked for.
    private static StepResult RefuseDrawn(AwaitingDraw waiting, string explanation) =>
        new(new PlayState(waiting), [new Refused(RefusalReason.Misplaced, explanation)]);
}

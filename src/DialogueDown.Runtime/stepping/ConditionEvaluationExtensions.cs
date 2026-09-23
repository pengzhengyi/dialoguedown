using DialogueDown.Playbook.Conditions;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Stepping;

/// <summary>
/// Judging a condition by what the world said.
/// </summary>
/// <remarks>
/// A condition asks the world about keys and the world answers them, which leaves the reading
/// itself: whether those answers add up to a yes. A key condition holds exactly when the key it
/// names was answered yes, so <c>`Hero.HasSword?`</c> answered
/// <c>{ "Hero.HasSword": true }</c> holds and the line it guards plays.
/// <para>
/// The answers have already been held to the keys the condition asked about, each answered with a
/// truth, so this takes them and reads. It is the last step of the three the world's reply goes
/// through — asked, held, read — and the only one that says what the answer means.
/// </para>
/// </remarks>
internal static class ConditionEvaluationExtensions
{
    /// <summary>Whether a condition holds, by what the world said.</summary>
    /// <param name="condition">The condition to judge.</param>
    /// <param name="supply">What the world said.</param>
    /// <returns><see langword="true"/> when whatever the condition guards may go ahead.</returns>
    /// <exception cref="NotSupportedException">
    /// The condition is of a kind nothing has been taught to read.
    /// </exception>
    public static bool Holds(this Condition condition, Supply supply)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(supply);

        return condition switch
        {
            KeyCondition key => supply.Holds(key.Key),

            // Every kind is named above, so a kind added to the format arrives here as a failure
            // rather than as a guard nothing knows how to read.
            _ => throw new NotSupportedException(
                $"No reading is defined for {condition.GetType().Name}."),
        };
    }

    /// <summary>Whether what a guard stands in front of may go ahead, by what the world said.</summary>
    /// <remarks>
    /// A line, a jump, an option, and a branch arm are each written with a guard or without one.
    /// Reading the two the same way here is what lets a caller ask whether a thing is allowed
    /// without first asking whether anybody guarded it.
    /// </remarks>
    /// <param name="guarded">The thing a guard may stand in front of.</param>
    /// <param name="supply">What the world said.</param>
    /// <returns><see langword="true"/> when nothing guards it, or its guard holds.</returns>
    public static bool IsAllowed(this IConditional guarded, Supply supply)
    {
        ArgumentNullException.ThrowIfNull(guarded);
        ArgumentNullException.ThrowIfNull(supply);

        return guarded.Condition is null || guarded.Condition.Holds(supply);
    }
}

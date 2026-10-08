using DialogueDown.Playbook.Conditions;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Stepping;

/// <summary>
/// Decides whether a condition holds, given the world's answers.
/// </summary>
/// <remarks>
/// A key condition holds exactly when the key it names was answered yes, so
/// <c>`Hero.HasSword?`</c> answered <c>{ "Hero.HasSword": true }</c> holds and the line it guards
/// plays. The supply has already been checked against the keys asked about, each answered with a
/// truth, so this only reads it.
/// </remarks>
internal static class ConditionEvaluationExtensions
{
    /// <summary>Whether a condition holds, by what the world said.</summary>
    /// <param name="condition">The condition to judge.</param>
    /// <param name="supply">What the world said.</param>
    /// <returns><see langword="true"/> when whatever the condition guards may go ahead.</returns>
    /// <exception cref="NotSupportedException">
    /// The condition is of a kind this method does not handle.
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

    /// <summary>Whether a node or arm may go ahead, given the world's answers.</summary>
    /// <param name="guarded">The node or arm to check.</param>
    /// <param name="supply">What the world said.</param>
    /// <returns><see langword="true"/> when nothing guards it, or its guard holds.</returns>
    public static bool IsAllowed(this IConditional guarded, Supply supply)
    {
        ArgumentNullException.ThrowIfNull(guarded);
        ArgumentNullException.ThrowIfNull(supply);

        return guarded.Condition is null || guarded.Condition.Holds(supply);
    }
}

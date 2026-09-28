using System.Collections.Immutable;

namespace DialogueDown.Playbook.Conditions;

/// <summary>
/// Reading what a condition asks the world.
/// </summary>
/// <remarks>
/// A condition is answered by the world, so a run has to know what to ask about before it can
/// judge one. A key condition asks about the single key it names, and the reading is a list so
/// that a condition composed of others can name every key it rests on.
/// </remarks>
public static class ConditionExtensions
{
    /// <summary>The keys a condition asks the world about.</summary>
    /// <param name="condition">The condition to read.</param>
    /// <returns>The keys, in the order the condition asks about them.</returns>
    /// <exception cref="NotSupportedException">
    /// The condition is of a kind this has never been taught to read.
    /// </exception>
    public static ImmutableArray<string> Keys(this Condition condition)
    {
        ArgumentNullException.ThrowIfNull(condition);

        return condition switch
        {
            KeyCondition key => [key.Key],

            // Every kind is named above, so a kind added to the format arrives here as a failure
            // rather than as a condition nobody knows what to ask about.
            _ => throw new NotSupportedException(
                $"No keys are defined for {condition.GetType().Name}."),
        };
    }
}

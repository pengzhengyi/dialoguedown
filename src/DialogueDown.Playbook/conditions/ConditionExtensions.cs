using System.Collections.Immutable;

namespace DialogueDown.Playbook.Conditions;

/// <summary>
/// Lists the keys a condition asks the world about.
/// </summary>
/// <remarks>
/// A run must ask the world about these keys before it can decide the condition. The condition
/// <c>`Alice.HasKey?`</c> gives the one key <c>Alice.HasKey</c>.
/// </remarks>
public static class ConditionExtensions
{
    /// <summary>The keys a condition asks the world about.</summary>
    /// <param name="condition">The condition to read.</param>
    /// <returns>The keys, in the order the condition asks about them.</returns>
    /// <exception cref="NotSupportedException">
    /// The condition is of a kind this method does not handle.
    /// </exception>
    public static ImmutableArray<string> Keys(this Condition condition)
    {
        ArgumentNullException.ThrowIfNull(condition);

        return condition switch
        {
            KeyCondition key => [key.Key],

            // Every kind is named above, so a kind added to the format fails here until it is
            // handled.
            _ => throw new NotSupportedException(
                $"No keys are defined for {condition.GetType().Name}."),
        };
    }
}

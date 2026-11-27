namespace DialogueDown.Playbook.Conditions;

/// <summary>Reads the condition of any node or edge that can carry one.</summary>
public static class ConditionalExtensions
{
    /// <summary>Whether a condition guards this.</summary>
    /// <param name="guarded">The node or edge to ask about.</param>
    /// <returns><see langword="true"/> when a condition guards it.</returns>
    public static bool IsConditional(this IConditional guarded)
    {
        ArgumentNullException.ThrowIfNull(guarded);

        return guarded.Condition is not null;
    }
}

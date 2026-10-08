namespace DialogueDown.Playbook.Conditions;

/// <summary>
/// Something a <see cref="Conditions.Condition"/> can guard — a line or a control block, and the
/// arms that may be withheld: a divert, an option, a random option, or one arm of a branch.
/// </summary>
/// <remarks>
/// A node's condition decides whether it plays at all; an arm's decides whether that way out is
/// taken. A succession edge has no condition: it is the fall-through, and is always available.
/// </remarks>
public interface IConditional
{
    /// <summary>Gets the condition guarding this, or <see langword="null"/> when nothing does.</summary>
    Condition? Condition { get; }
}

/// <summary>Reads the condition of any node or arm that can carry one.</summary>
public static class ConditionalExtensions
{
    /// <summary>Whether a condition guards this.</summary>
    /// <param name="guarded">The node or arm to ask about.</param>
    /// <returns><see langword="true"/> when a condition guards it.</returns>
    public static bool IsConditional(this IConditional guarded)
    {
        ArgumentNullException.ThrowIfNull(guarded);

        return guarded.Condition is not null;
    }
}

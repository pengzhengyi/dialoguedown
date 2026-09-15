namespace DialogueDown.Playbook.Conditions;

/// <summary>
/// Something a <see cref="Conditions.Condition"/> can guard — a line or a control block, and the
/// arms that may be withheld: a divert, an option, a random option, or one arm of a branch.
/// </summary>
/// <remarks>
/// A node's condition decides whether it plays at all; an arm's decides whether that way out is
/// taken. Both ask the world the same kind of question, so both are read the same way, and a
/// reader looking for a guard asks once rather than naming every kind that can carry one. A
/// succession is not one of these: it is the fall-through, and is always available.
/// </remarks>
public interface IConditional
{
    /// <summary>Gets the condition guarding this, or <see langword="null"/> when nothing does.</summary>
    Condition? Condition { get; }
}

/// <summary>Asking about a guard without naming the kind that carries it.</summary>
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

namespace DialogueDown.Playbook.Conditions;

/// <summary>
/// Something a <see cref="Conditions.Condition"/> can guard — a line or a control block, and the
/// edges that may be withheld: a divert, an option, a random option, or one branch of a block
/// condition.
/// </summary>
/// <remarks>
/// A node's condition decides whether it plays at all; an edge's decides whether that way out is
/// taken. A succession edge has no condition: it is the fall-through, and is always available.
/// </remarks>
public interface IConditional
{
    /// <summary>Gets the condition guarding this, or <see langword="null"/> when nothing does.</summary>
    Condition? Condition { get; }
}

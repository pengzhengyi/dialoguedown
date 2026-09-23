namespace DialogueDown.Runtime.Protocol;

/// <summary>
/// Why the run could not take what it was given.
/// </summary>
/// <remarks>
/// A reason is what a fixture compares: it comes from a closed set, so two runtimes either agree
/// or they do not, while the explanation a run words for itself stays free to differ.
/// </remarks>
public enum RefusalReason
{
    /// <summary><c>Next</c> arrived before <c>Start</c>, so there is nothing to advance from.</summary>
    NotStarted,

    /// <summary><c>Next</c> arrived after the run had ended.</summary>
    AlreadyEnded,

    /// <summary>
    /// A command the protocol defines arrived where the run cannot take it — <c>Next</c> while the
    /// run waits on the host, or <c>Done</c> or <c>Failed</c> when nothing was asked of it.
    /// </summary>
    Misplaced,

    /// <summary>The command is one this runner does not define.</summary>
    UnknownCommand,

    /// <summary>The node the run stands at has no way onward.</summary>
    LeadsNowhere,

    /// <summary>A walk entered a ring of nodes that hand the host nothing.</summary>
    EndlessRing,

    /// <summary>A node or edge plays only on an answer nobody can give yet.</summary>
    /// <remarks>
    /// Temporary: this reason goes when the run can ask the world, and a condition is answered
    /// rather than refused.
    /// </remarks>
    UnansweredCondition,

    /// <summary>The world was asked about a key and did not answer it.</summary>
    UnansweredKey,

    /// <summary>The world answered a key that nothing had asked about.</summary>
    UnaskedKey,

    /// <summary>The world answered a key with something other than the kind its use needs.</summary>
    WrongAnswerKind,

    /// <summary>One key on a node is needed as a truth and as words both.</summary>
    /// <remarks>
    /// A key is asked about once, so a key that both guards a node and stands in what it says has
    /// one answer to serve two uses, and whichever kind comes back leaves the other unreadable.
    /// </remarks>
    KeyNeededBothWays,

    /// <summary>The node kind is one this build has not learned to play.</summary>
    UnplayableNode,
}

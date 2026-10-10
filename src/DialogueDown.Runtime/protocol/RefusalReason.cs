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
    /// A command the protocol defines arrived where the run cannot take it, such as <c>Next</c>
    /// while the run waits on the host, <c>Done</c> when no <c>Perform</c> is waiting, or
    /// <c>Choose</c> away from a menu.
    /// </summary>
    Misplaced,

    /// <summary>The command is one this runner does not define.</summary>
    UnknownCommand,

    /// <summary>The node the run stands at has no way onward.</summary>
    LeadsNowhere,

    /// <summary>
    /// The run entered a loop of nodes that give the host nothing, so it would never stop.
    /// </summary>
    EndlessLoop,

    /// <summary>The world was asked about a key and did not answer it.</summary>
    UnansweredKey,

    /// <summary>The world answered a key that nothing had asked about.</summary>
    UnaskedKey,

    /// <summary>The world answered a key with something other than the kind its use needs.</summary>
    WrongAnswerKind,

    /// <summary>One key on a node is needed as a truth and as words both.</summary>
    /// <remarks>
    /// A key is asked about once, so in <c>`Alice.HasKey?` Alice: You have `"Alice.HasKey"`.</c>
    /// one answer would have to be both a truth and words.
    /// </remarks>
    KeyNeededBothWays,

    /// <summary>The node is of a kind this build cannot play.</summary>
    UnplayableNode,

    /// <summary><c>Choose</c> named a position outside the options the menu offered.</summary>
    NoSuchOption,
}

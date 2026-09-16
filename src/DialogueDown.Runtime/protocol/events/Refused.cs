namespace DialogueDown.Runtime.Protocol;

/// <summary>
/// The command could not be taken, and nothing moved.
/// </summary>
/// <param name="Reason">Why it could not be taken, from the protocol's closed set.</param>
/// <param name="Explanation">What is wrong, naming what was sent and why it did not fit.</param>
public sealed record Refused(RefusalReason Reason, string Explanation) : Event;

namespace DialogueDown.Runtime.Protocol;

/// <summary>
/// The command could not be taken, and nothing moved.
/// </summary>
/// <param name="Because">What is wrong, naming what was sent and why it did not fit.</param>
public sealed record Refused(string Because) : Event;

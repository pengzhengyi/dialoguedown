using DialogueDown.Playbook.Speech;

namespace DialogueDown.Runtime.Protocol;

/// <summary>
/// Asks the host to carry out a command, and to send <see cref="Done"/> when it has.
/// </summary>
/// <remarks>
/// Named for what it asks: at the moment it is sent, nothing has been done yet. One step may send
/// several, in the order they were written, and then waits for a single <see cref="Done"/>: a
/// command only changes the world and never reads it, so none needs the one before it finished.
/// </remarks>
/// <param name="Effect">What the host is being asked to carry out, as the playbook names it.</param>
public sealed record Perform(SpeechFragment Effect) : Request;

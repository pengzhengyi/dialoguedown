using DialogueDown.Playbook.Speech;

namespace DialogueDown.Runtime.Protocol;

/// <summary>
/// Carry this out, and say when it is done.
/// </summary>
/// <remarks>
/// Named for what it asks: at the moment it is sent, nobody has done anything yet. A node
/// carrying several effects asks for each of them, in the order they were written, and waits
/// once at the end — they were authored as one line, and an effect only ever writes, so none
/// of them needs the world settled before the next.
/// </remarks>
/// <param name="Effect">What the host is being asked to carry out, as the playbook names it.</param>
public sealed record Perform(SpeechFragment Effect) : Request;

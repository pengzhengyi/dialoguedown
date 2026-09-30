using System.Collections.Immutable;
using DialogueDown.Playbook.Speech;

namespace DialogueDown.Runtime.Protocol;

/// <summary>
/// The speaker went on with the line, after a command.
/// </summary>
/// <remarks>
/// A line with a command in it reaches the host in parts, so each command arrives where it was
/// written. The line opens with a <see cref="Said"/>, and the words after each command that say
/// something arrive as one of these. It names no speaker, because whoever opened the line is still
/// the one speaking.
/// </remarks>
/// <param name="Speech">What was said, as the playbook holds it.</param>
public sealed record Continued(ImmutableArray<SpeechFragment> Speech) : Event;

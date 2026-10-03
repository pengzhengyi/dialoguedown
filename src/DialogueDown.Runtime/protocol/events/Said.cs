using System.Collections.Immutable;
using DialogueDown.Playbook.Speech;

namespace DialogueDown.Runtime.Protocol;

/// <summary>
/// Somebody said something.
/// </summary>
/// <remarks>
/// Every line that plays opens with one. It carries the words before the line's first command,
/// which are none when a command opens the line, and it is sent even then, so the host knows who
/// is acting before any command arrives.
/// </remarks>
/// <param name="Speaker">Who said it, by name, or <see langword="null"/> for the anonymous default speaker.</param>
/// <param name="Speech">What was said, as the playbook holds it.</param>
public sealed record Said(string? Speaker, ImmutableArray<SpeechFragment> Speech) : Event;

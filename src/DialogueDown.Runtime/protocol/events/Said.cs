using System.Collections.Immutable;
using DialogueDown.Playbook.Speech;

namespace DialogueDown.Runtime.Protocol;

/// <summary>
/// Somebody said something.
/// </summary>
/// <param name="Speaker">Who said it, by name, or <see langword="null"/> for the anonymous default speaker.</param>
/// <param name="Speech">What was said, as the playbook holds it.</param>
public sealed record Said(string? Speaker, ImmutableArray<SpeechFragment> Speech) : Event;

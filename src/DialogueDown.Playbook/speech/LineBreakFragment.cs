namespace DialogueDown.Playbook.Speech;

/// <summary>
/// A place where the source wrapped onto a new line within one line of speech.
/// </summary>
/// <remarks>
/// <code>
/// Bob: This is the night view of the Huangpu River.
/// It is *beautiful*, especially at dusk.
/// </code>
/// is one line with a break after <c>River.</c> A hard break, such as a backslash at the end of a
/// source line, starts a new line instead. It carries nothing: the kind is the whole fragment.
/// </remarks>
public sealed record LineBreakFragment : SpeechFragment;

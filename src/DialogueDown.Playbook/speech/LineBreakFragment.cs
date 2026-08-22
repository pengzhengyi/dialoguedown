namespace DialogueDown.Playbook.Speech;

/// <summary>
/// A break within a line — a hard wrap the writer asked for.
/// </summary>
/// <remarks>It carries nothing: the kind is the whole fragment.</remarks>
public sealed record LineBreakFragment : SpeechFragment;

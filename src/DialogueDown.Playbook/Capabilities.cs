namespace DialogueDown.Playbook;

/// <summary>
/// The named constructs a runtime must understand to play a playbook correctly.
/// </summary>
/// <remarks>
/// This is the vocabulary that may appear on the wire, shared by whoever writes a playbook and
/// whoever reads one. Which of these names a given build actually honors is a separate question,
/// answered by <see cref="PlaybookSupport"/>.
/// </remarks>
public static class Capabilities
{
    /// <summary>Every construct the compiler emits. Always required.</summary>
    public const string Core = "core";

    /// <summary>
    /// A reference into another script. Reserved: the compiler does not emit it and this build does
    /// not read it, so every node reference is an index within one script.
    /// </summary>
    public const string CrossFileJump = "cross-file-jump";
}

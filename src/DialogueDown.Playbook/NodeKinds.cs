namespace DialogueDown.Playbook;

/// <summary>
/// The <c>kind</c> values that tag a node on the wire.
/// </summary>
/// <remarks>
/// Separate from <see cref="EdgeKinds"/> rather than one flat list, because a branch is both a
/// node and an edge and the two mean different things.
/// </remarks>
public static class NodeKinds
{
    /// <summary>A line somebody says.</summary>
    public const string Line = "line";

    /// <summary>A menu the player picks from.</summary>
    public const string Choice = "choice";

    /// <summary>A choice the engine draws instead.</summary>
    public const string RandomChoice = "random-choice";

    /// <summary>A block condition fanning out to its arms.</summary>
    public const string Branch = "branch";

    /// <summary>An effect-only line, with no speaker.</summary>
    public const string Control = "control";

    /// <summary>Where a run stops.</summary>
    public const string End = "end";
}

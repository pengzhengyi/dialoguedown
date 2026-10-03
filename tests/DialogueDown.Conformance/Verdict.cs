namespace DialogueDown.Conformance;

/// <summary>
/// What a reader must do with a fixture's playbook.
/// </summary>
/// <remarks>
/// Each member's wire name is pinned in <see cref="VerdictConverter"/>, so renaming a member
/// cannot change the format other runtimes read.
/// </remarks>
public enum Verdict
{
    /// <summary>The document is playable exactly as written.</summary>
    Accept,

    /// <summary>The document cannot be played, and a reader must say so rather than play it anyway.</summary>
    Refuse,
}

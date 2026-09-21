using DialogueDown.Diagnostics;

namespace DialogueDown.Cli.Fixing;

/// <summary>What happened to a diagnostic's preferred fix during a fix run.</summary>
/// <param name="Fix">The preferred fix the diagnostic offered.</param>
/// <param name="SkipReason">Why it was skipped, or <c>null</c> when it was applied.</param>
internal sealed record FixOutcome(LocatedFix Fix, FixSkipReason? SkipReason)
{
    /// <summary>Whether the fix was written into the corrected text.</summary>
    public bool Applied => SkipReason is null;

    /// <summary>Records that the fix was applied.</summary>
    public static FixOutcome Apply(LocatedFix fix) => new(fix, null);

    /// <summary>Records that the fix was skipped for the given reason.</summary>
    public static FixOutcome Skip(LocatedFix fix, FixSkipReason reason) => new(fix, reason);
}

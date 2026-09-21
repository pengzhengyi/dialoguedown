namespace DialogueDown.Cli.Fixing;

/// <summary>
/// The result of applying a compile's preferred fixes: the corrected text and, in the compile's
/// diagnostic order, each diagnostic with its fix outcome.
/// </summary>
internal sealed record FixApplication(string Text, IReadOnlyList<ReportedDiagnostic> Reported)
{
    /// <summary>Whether any diagnostic carries a fix, so there is anything to report.</summary>
    public bool HasCandidates => Reported.Any(reported => reported.Fix is not null);

    /// <summary>How many fixes were written into <see cref="Text"/>.</summary>
    public int AppliedCount => Reported.Count(reported => reported.Fix is { Applied: true });
}

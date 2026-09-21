using DialogueDown.Diagnostics;

namespace DialogueDown.Cli.Fixing;

/// <summary>The facts a fix run adds to the report, beyond each diagnostic's own outcome.</summary>
/// <param name="Remaining">How many diagnostics the corrected script still reports.</param>
/// <param name="WrittenFile">The script corrected on disk, or <c>null</c> when nothing was written.</param>
/// <param name="CorrectedSource">The corrected text, which locates anything new after fixing.</param>
/// <param name="NewAfterFixing">
/// Diagnostics the corrected script reports that the script as read did not.
/// </param>
internal sealed record FixSummary(
    int Remaining,
    string? WrittenFile,
    string CorrectedSource,
    IReadOnlyList<LocatedDiagnostic> NewAfterFixing);

using DialogueDown.Diagnostics;

namespace DialogueDown.Cli.Fixing;

/// <summary>The facts a fix run adds to the report, after the diagnostics as found.</summary>
/// <param name="Outcomes">What happened to each candidate, in ascending position order.</param>
/// <param name="WrittenFile">The script corrected on disk, or <c>null</c> when nothing was written.</param>
/// <param name="Remaining">The corrected script's diagnostics, which phrase what is left.</param>
/// <param name="NewAfterFixing">
/// Diagnostics the corrected script reports that the script as read did not.
/// </param>
/// <param name="CorrectedSource">The corrected text, which locates anything new after fixing.</param>
internal sealed record FixRun(
    IReadOnlyList<FixOutcome> Outcomes,
    string? WrittenFile,
    IReadOnlyList<LocatedDiagnostic> Remaining,
    IReadOnlyList<LocatedDiagnostic> NewAfterFixing,
    string CorrectedSource);

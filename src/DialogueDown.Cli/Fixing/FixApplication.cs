using DialogueDown.Diagnostics;

namespace DialogueDown.Cli.Fixing;

/// <summary>
/// The result of applying a compile's preferred fixes: the corrected text and what happened to
/// each candidate, in ascending position order.
/// </summary>
internal sealed record FixApplication(string Text, IReadOnlyList<FixOutcome> Outcomes)
{
    /// <summary>Whether any diagnostic carries a fix, so there is anything to report.</summary>
    public bool HasCandidates => Outcomes.Count > 0;

    /// <summary>How many fixes were written into <see cref="Text"/>.</summary>
    public int AppliedCount => Outcomes.Count(outcome => outcome.Applied);

    /// <summary>
    /// The diagnostics of the corrected script that the script as found did not have.
    /// </summary>
    /// <remarks>
    /// A diagnostic is matched by its code and by where it starts in the script as found, so one
    /// that a fix only moved is not new. A diagnostic whose fix was applied is expected to be gone:
    /// finding it again means the fix did not clear it, so it is new as well.
    /// </remarks>
    /// <param name="asFound">The diagnostics of the script before fixing.</param>
    /// <param name="corrected">The diagnostics of <see cref="Text"/>.</param>
    public IReadOnlyList<LocatedDiagnostic> NewDiagnostics(
        IReadOnlyList<LocatedDiagnostic> asFound, IReadOnlyList<LocatedDiagnostic> corrected)
    {
        ArgumentNullException.ThrowIfNull(asFound);
        ArgumentNullException.ThrowIfNull(corrected);

        var applied = Outcomes.Where(outcome => outcome.Applied).Select(outcome => outcome.Fix).ToHashSet();
        var remaining = asFound
            .Where(diagnostic => !diagnostic.Fixes.Any(applied.Contains))
            .Select(diagnostic => (diagnostic.Code, (int?)diagnostic.StartOffset))
            .ToHashSet();
        return
        [
            .. corrected.Where(diagnostic =>
                !remaining.Contains((diagnostic.Code, OriginalOffset(diagnostic.StartOffset)))),
        ];
    }

    // Where an offset in Text was in the script as found, or null inside text a fix wrote. Each
    // applied edit before the offset moves it by the edit's change in length.
    private int? OriginalOffset(int correctedOffset)
    {
        var shift = 0;
        foreach (var edit in AppliedEdits())
        {
            var writtenStart = edit.StartOffset + shift;
            if (correctedOffset < writtenStart)
            {
                break;
            }

            if (correctedOffset < writtenStart + edit.NewText.Length)
            {
                return null;
            }

            shift += edit.NewText.Length - (edit.EndOffset - edit.StartOffset);
        }

        return correctedOffset - shift;
    }

    private IEnumerable<LocatedEdit> AppliedEdits() =>
        Outcomes
            .Where(outcome => outcome.Applied)
            .SelectMany(outcome => outcome.Fix.Edits)
            .OrderBy(edit => edit.StartOffset)
            .ThenBy(edit => edit.EndOffset);
}

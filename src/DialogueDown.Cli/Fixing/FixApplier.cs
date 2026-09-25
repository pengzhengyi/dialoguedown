using DialogueDown.Diagnostics;

namespace DialogueDown.Cli.Fixing;

/// <summary>
/// Applies a compile's preferred diagnostic fixes to the script text. The text as read and the
/// diagnostics as found are the whole input — edits carry absolute offsets into that text — so
/// the applier is a pure function and never touches the file system.
/// </summary>
/// <remarks>
/// A diagnostic's <see cref="LocatedDiagnostic.Fixes"/> is a list of alternatives, not a plan: its
/// first element is the preferred, auto-applicable repair, and the rest are choices for the
/// writer. Candidates are considered in ascending position with a fixed tie-break, so the
/// compiler's emission order never decides which fix wins; the earliest fix in the file keeps a
/// conflict, and it is applied as one atomic splice against the original offsets.
/// </remarks>
internal static class FixApplier
{
    /// <summary>Applies every preferred fix that can be applied, and reports each outcome.</summary>
    public static FixApplication Apply(string source, IReadOnlyList<LocatedDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(diagnostics);

        var outcomes = new List<FixOutcome>();
        var kept = new List<LocatedFix>();
        foreach (var fix in PreferredFixes(diagnostics))
        {
            var skipReason = SkipReason(fix, source.Length, kept);
            outcomes.Add(skipReason is null ? FixOutcome.Apply(fix) : FixOutcome.Skip(fix, skipReason.Value));
            if (skipReason is null)
            {
                kept.Add(fix);
            }
        }

        return new FixApplication(Splice(source, kept), outcomes);
    }

    /// <summary>Splices the given fixes into the text, descending so no offset shifts.</summary>
    internal static string Splice(string source, IReadOnlyList<LocatedFix> fixes)
    {
        var edits = fixes
            .SelectMany(fix => fix.Edits)
            .OrderByDescending(edit => edit.StartOffset)
            .ThenByDescending(edit => edit.EndOffset)
            .ToList();

        var text = source;
        foreach (var edit in edits)
        {
            text = string.Concat(text.AsSpan(0, edit.StartOffset), edit.NewText, text.AsSpan(edit.EndOffset));
        }

        return text;
    }

    private static IEnumerable<LocatedFix> PreferredFixes(IReadOnlyList<LocatedDiagnostic> diagnostics) =>
        diagnostics
            .Where(diagnostic => diagnostic.Fixes.Count > 0)
            .Select(diagnostic => (Diagnostic: diagnostic, Fix: diagnostic.Fixes[0]))
            .OrderBy(candidate => candidate.Fix.Edits[0].StartOffset)
            .ThenBy(candidate => candidate.Diagnostic.Code, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.Fix.Title, StringComparer.Ordinal)
            .Select(candidate => candidate.Fix);

    private static FixSkipReason? SkipReason(LocatedFix fix, int length, IReadOnlyList<LocatedFix> kept)
    {
        if (fix.Edits.Any(edit =>
            edit.StartOffset < 0 || edit.EndOffset < edit.StartOffset || edit.EndOffset > length))
        {
            return FixSkipReason.OutsideTheScript;
        }

        return kept.Any(keptFix => Overlaps(fix, keptFix)) ? FixSkipReason.OverlapsAnAppliedFix : null;
    }

    private static bool Overlaps(LocatedFix candidate, LocatedFix kept) =>
        candidate.Edits.Any(edit => kept.Edits.Any(keptEdit => Overlaps(edit, keptEdit)));

    // Half-open ranges, with a zero-width edit (an insertion) widened by one so that an insertion
    // and a replacement at the same offset conflict, the way clang-tidy's overlap sweep treats them.
    private static bool Overlaps(LocatedEdit left, LocatedEdit right) =>
        left.StartOffset < EndOf(right) && right.StartOffset < EndOf(left);

    private static int EndOf(LocatedEdit edit) => Math.Max(edit.EndOffset, edit.StartOffset + 1);
}

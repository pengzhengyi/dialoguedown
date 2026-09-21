namespace DialogueDown.Cli;

/// <summary>Renders a compile's located diagnostics for the reader.</summary>
internal interface IErrataRenderer
{
    /// <summary>
    /// Writes each reported diagnostic in <paramref name="reported"/>, sorted by position then
    /// code, followed by a summary and, when any diagnostic still carries an unapplied fix, a
    /// fixability hint. A fixed or skipped diagnostic carries its outcome above its reference
    /// line. On an interactive console it renders rich Errata blocks with a source snippet and
    /// caret over <paramref name="source"/>; otherwise it writes the greppable
    /// <c>file(line,column): severity CODE: message</c> one-liner. When <paramref name="fix"/>
    /// describes a fix run, the summary names fixed versus remaining diagnostics, a write notice
    /// names the corrected file, and anything that appeared only after fixing follows. Writes
    /// nothing when there is nothing to report.
    /// </summary>
    void Render(
        string file,
        string source,
        IReadOnlyList<ReportedDiagnostic> reported,
        FixSummary? fix = null);
}

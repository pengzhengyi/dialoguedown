using DialogueDown.Cli.Fixing;
using DialogueDown.Diagnostics;

namespace DialogueDown.Cli;

/// <summary>Renders a compile's located diagnostics for the reader.</summary>
internal interface IErrataRenderer
{
    /// <summary>
    /// Writes each diagnostic in <paramref name="diagnostics"/>, sorted by position then code,
    /// followed by a summary and, when any carries an unapplied fix, a fixability hint — exactly
    /// what a plain compile prints. On an interactive console it renders rich Errata blocks with a
    /// source snippet and caret over <paramref name="source"/>; otherwise it writes the greppable
    /// <c>file(line,column): severity CODE: message</c> one-liner.
    /// </summary>
    /// <param name="file">The script's path, as the report names it.</param>
    /// <param name="source">The script text the diagnostics locate.</param>
    /// <param name="diagnostics">The diagnostics as found.</param>
    /// <param name="fix">
    /// When a fix run had candidates, its outcome: the write notice, each applied fix with its
    /// hunk, each skipped fix, and anything that appeared only after fixing. Nothing is written
    /// when there is nothing to report at all.
    /// </param>
    void Render(
        string file,
        string source,
        IReadOnlyList<LocatedDiagnostic> diagnostics,
        FixRun? fix = null);
}

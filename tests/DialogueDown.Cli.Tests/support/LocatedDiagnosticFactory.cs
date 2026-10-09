using DialogueDown.Diagnostics;

namespace DialogueDown.Cli.Tests.Support;

/// <summary>
/// Diagnostics as a compile reports them, with the fixes and edits they carry, built by hand so
/// a test of rendering or fixing names only the code, the place, and the fix it is about.
/// </summary>
internal static class LocatedDiagnosticFactory
{
    /// <summary>The title the compiler gives the fix that escapes a stray sigil.</summary>
    public const string EscapeTitle = "Escape as literal text";

    /// <summary>A diagnostic at one line and column, for a test that reads where it is printed.</summary>
    /// <param name="code">The diagnostic's code, such as <c>DLG1113</c>.</param>
    /// <param name="line">The 1-based line it points at.</param>
    /// <param name="column">The 1-based column it points at.</param>
    /// <param name="message">What it says.</param>
    /// <param name="severity">How serious it is.</param>
    /// <returns>A zero-width diagnostic whose offsets are not read.</returns>
    public static LocatedDiagnostic At(
        string code,
        int line,
        int column,
        string message = "a problem",
        DiagnosticSeverity severity = DiagnosticSeverity.Warning) =>
        new(
            code,
            severity,
            DiagnosticCategory.Syntax,
            message,
            new LinePosition(line, column),
            new LinePosition(line, column),
            StartOffset: 0,
            EndOffset: 0);

    /// <summary>A diagnostic over a range of <paramref name="source"/>, placed by its offsets.</summary>
    /// <param name="source">The script the diagnostic is about.</param>
    /// <param name="start">The offset where the flagged text starts.</param>
    /// <param name="end">The offset just past it; equal to <paramref name="start"/> for a point.</param>
    /// <param name="code">The diagnostic's code.</param>
    /// <param name="message">What it says.</param>
    /// <param name="severity">How serious it is.</param>
    /// <param name="category">Which stage of the compiler found it.</param>
    /// <returns>A diagnostic whose lines and columns agree with its offsets in the source.</returns>
    public static LocatedDiagnostic Over(
        string source,
        int start,
        int end,
        string code,
        string message = "a problem",
        DiagnosticSeverity severity = DiagnosticSeverity.Warning,
        DiagnosticCategory category = DiagnosticCategory.Syntax) =>
        new(code, severity, category, message, Locate(source, start), Locate(source, end), start, end);

    /// <summary>A diagnostic over offsets on the first line, for a test that applies its fixes.</summary>
    /// <param name="code">The diagnostic's code.</param>
    /// <param name="start">The offset where the flagged text starts.</param>
    /// <param name="end">The offset just past it.</param>
    /// <param name="fixes">The fixes it offers, the preferred one first.</param>
    /// <returns>A warning whose columns follow its offsets.</returns>
    public static LocatedDiagnostic Spanning(string code, int start, int end, params LocatedFix[] fixes) =>
        new(
            code,
            DiagnosticSeverity.Warning,
            DiagnosticCategory.Syntax,
            $"{code} problem",
            new LinePosition(1, start + 1),
            new LinePosition(1, end + 1),
            start,
            end)
        {
            Fixes = fixes,
        };

    /// <summary>A fix made of the given edits.</summary>
    /// <param name="title">What the fix is called where it is listed.</param>
    /// <param name="edits">The edits it makes, applied together.</param>
    /// <returns>The fix.</returns>
    public static LocatedFix Fix(string title, params LocatedEdit[] edits) => new(title, edits);

    /// <summary>The fix that escapes a sigil by writing a backslash before it.</summary>
    /// <param name="at">The offset of the sigil.</param>
    /// <returns>The escape the compiler offers for a stray <c>=&gt;</c>.</returns>
    public static LocatedFix Escape(int at) => Fix(EscapeTitle, Insert(at, "\\"));

    /// <summary>An edit that writes <paramref name="text"/> at an offset and removes nothing.</summary>
    /// <param name="at">Where the text goes.</param>
    /// <param name="text">What is written.</param>
    /// <returns>The edit.</returns>
    public static LocatedEdit Insert(int at, string text) => new(at, at, text);

    /// <summary>An edit that writes <paramref name="text"/> in place of a range.</summary>
    /// <param name="start">Where the replaced text starts.</param>
    /// <param name="end">The offset just past it.</param>
    /// <param name="text">What is written instead.</param>
    /// <returns>The edit.</returns>
    public static LocatedEdit Replace(int start, int end, string text) => new(start, end, text);

    private static LinePosition Locate(string source, int offset)
    {
        var before = source[..offset];
        var lineStart = before.LastIndexOf('\n') + 1;
        return new LinePosition(before.Count(character => character == '\n') + 1, offset - lineStart + 1);
    }
}

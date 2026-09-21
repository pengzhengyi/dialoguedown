using DialogueDown.Diagnostics;
using Errata;
using Spectre.Console;

namespace DialogueDown.Cli;

/// <summary>
/// Renders a compile's located diagnostics and the outcome of their fixes. On an interactive
/// console it uses the <see href="https://github.com/spectreconsole/errata">Errata</see> library
/// to draw a rich block per diagnostic — the source line with a colored caret under the offending
/// range — and otherwise writes a greppable
/// <c>file(line,column): severity CODE: message</c> one-liner. Both paths end with a summary, a
/// fixability hint, and, for a fix run, the write notice and anything new after fixing. Rendering
/// stays confined to the CLI (the umbrella note's DD7).
/// </summary>
internal sealed class ErrataRenderer(IAnsiConsole console) : IErrataRenderer
{
    public void Render(
        string file,
        string source,
        IReadOnlyList<ReportedDiagnostic> reported,
        FixSummary? fix = null)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(reported);
        if (reported.Count == 0)
        {
            return;
        }

        RenderDiagnostics(console, file, source, reported);
        var applied = reported.Count(entry => entry.Fix is { Applied: true });
        console.MarkupLineInterpolated($"[grey]{Summarize(reported)}{TallySuffix(fix, applied)}[/]");

        if (fix is { WrittenFile: { } written })
        {
            console.MarkupLineInterpolated($"[grey]Fixed {written} ({FixesCount(applied)})[/]");
        }

        var fixable = reported.Count(entry =>
            entry.Diagnostic.Fixes.Count > 0 && entry.Fix is not { Applied: true });
        if (fixable > 0)
        {
            console.MarkupLineInterpolated($"[grey]{fixable} fixable with --fix[/]");
        }

        if (fix is { NewAfterFixing.Count: > 0 })
        {
            console.MarkupLine("[grey]after fixing:[/]");
            RenderDiagnostics(
                console,
                file,
                fix.CorrectedSource,
                [.. fix.NewAfterFixing.Select(diagnostic => new ReportedDiagnostic(diagnostic, null))]);
        }
    }

    private static void RenderDiagnostics(
        IAnsiConsole console, string file, string source, IReadOnlyList<ReportedDiagnostic> reported)
    {
        var ordered = Ordered(reported).ToList();
        if (!console.Profile.Capabilities.Interactive || !TryRenderRich(console, file, source, ordered))
        {
            RenderPlain(console, file, ordered);
        }
    }

    // The rich, source-context rendering. Returns false (so the caller falls back to the one-liner)
    // if Errata cannot render a span — a failed report must never crash the compile command.
    private static bool TryRenderRich(
        IAnsiConsole console, string file, string source, IReadOnlyList<ReportedDiagnostic> reported)
    {
        try
        {
            var repository = new InMemorySourceRepository();
            repository.Register(file, source);
            var report = new Report(repository);
            foreach (var entry in reported)
            {
                var label = new Label(file, LabelSpan(entry.Diagnostic, source), entry.Diagnostic.Code)
                    .WithColor(SpectreColorOf(entry.Diagnostic.Severity));
                report.Diagnostics.Add(ErrataDiagnosticOf(entry).WithLabel(label));
            }

            report.Render(console, new ReportSettings { PropagateExceptions = true });
            console.WriteLine(); // separate the last block from the summary line
            return true;
        }
        catch (ErrataException)
        {
            return false;
        }
    }

    private static void RenderPlain(
        IAnsiConsole console, string file, IReadOnlyList<ReportedDiagnostic> reported)
    {
        foreach (var entry in reported)
        {
            var diagnostic = entry.Diagnostic;
            var color = ColorOf(diagnostic.Severity);
            var location = $"{file}({diagnostic.Start.Line},{diagnostic.Start.Column})";
            console.MarkupLineInterpolated(
                $"[{color}]{location}: {LabelOf(diagnostic.Severity)} {diagnostic.Code}: {diagnostic.Message}[/]");
            if (entry.Fix is { } fix)
            {
                console.MarkupLineInterpolated($"[grey]  {OutcomeLine(fix)}[/]");
            }

            console.MarkupLineInterpolated(
                $"[grey]  for more information, see {DiagnosticDocumentation.UrlFor(diagnostic.Code)}[/]");
        }
    }

    // An Errata span within the source, non-decreasing; a zero-width (synthetic) span is widened by
    // one where possible so there is a caret to draw.
    private static TextSpan LabelSpan(LocatedDiagnostic diagnostic, string source)
    {
        var start = Math.Clamp(diagnostic.StartOffset, 0, source.Length);
        var end = Math.Clamp(diagnostic.EndOffset, start, source.Length);
        if (end == start && end < source.Length)
        {
            end = start + 1;
        }

        return new TextSpan(start, end);
    }

    // Errata carries one note per diagnostic, so the fix outcome rides above the reference line
    // rather than growing a second affinity channel.
    private static Diagnostic ErrataDiagnosticOf(ReportedDiagnostic reported)
    {
        var diagnostic = reported.Diagnostic;
        var errata = diagnostic.Severity switch
        {
            DiagnosticSeverity.Error => Diagnostic.Error(diagnostic.Message),
            DiagnosticSeverity.Warning => Diagnostic.Warning(diagnostic.Message),
            _ => Diagnostic.Info(diagnostic.Message),
        };

        // Errata shows the category in place of the severity word, so combine them into a natural
        // phrase — "syntax error", "semantic warning" — to keep both visible (the color still
        // conveys severity). The greppable one-liner stays clean; only this rich header carries it.
        var header = $"{diagnostic.Category} {LabelOf(diagnostic.Severity)}".ToLowerInvariant();
        return errata
            .WithCode(diagnostic.Code)
            .WithCategory(header)
            .WithNote(NoteOf(reported));
    }

    private static string NoteOf(ReportedDiagnostic reported)
    {
        var reference = $"for more information, see {DiagnosticDocumentation.UrlFor(reported.Diagnostic.Code)}";
        return reported.Fix is { } fix ? $"{OutcomeLine(fix)}\n{reference}" : reference;
    }

    private static string OutcomeLine(FixOutcome fix) =>
        fix.Applied
            ? $"fix applied: {fix.Fix.Title}"
            : $"fix skipped: {fix.Fix.Title} ({PhraseOf(fix.SkipReason!.Value)})";

    private static string PhraseOf(FixSkipReason reason) => reason switch
    {
        FixSkipReason.OverlapsAnAppliedFix => "overlaps an applied fix",
        FixSkipReason.OutsideTheScript => "falls outside the script",
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unknown fix skip reason."),
    };

    private static string TallySuffix(FixSummary? fix, int applied) =>
        fix is null ? string.Empty : $" ({applied} fixed, {fix.Remaining} remaining)";

    private static string FixesCount(int applied) => applied == 1 ? "1 fix" : $"{applied} fixes";

    private static IEnumerable<ReportedDiagnostic> Ordered(IReadOnlyList<ReportedDiagnostic> reported) =>
        reported
            .OrderBy(entry => entry.Diagnostic.Start.Line)
            .ThenBy(entry => entry.Diagnostic.Start.Column)
            .ThenBy(entry => entry.Diagnostic.Code, StringComparer.Ordinal);

    private static string ColorOf(DiagnosticSeverity severity) => severity switch
    {
        DiagnosticSeverity.Error => "red",
        DiagnosticSeverity.Warning => "yellow",
        _ => "cyan",
    };

    private static Color SpectreColorOf(DiagnosticSeverity severity) => severity switch
    {
        DiagnosticSeverity.Error => Color.Red,
        DiagnosticSeverity.Warning => Color.Yellow,
        _ => Color.Aqua,
    };

    private static string LabelOf(DiagnosticSeverity severity) => severity switch
    {
        DiagnosticSeverity.Error => "error",
        DiagnosticSeverity.Warning => "warning",
        _ => "info",
    };

    private static string Summarize(IReadOnlyList<ReportedDiagnostic> reported)
    {
        var parts = new List<string>();
        Count(reported, DiagnosticSeverity.Error, "error", parts);
        Count(reported, DiagnosticSeverity.Warning, "warning", parts);
        Count(reported, DiagnosticSeverity.Info, "info", parts);
        return string.Join(", ", parts);
    }

    private static void Count(
        IReadOnlyList<ReportedDiagnostic> reported,
        DiagnosticSeverity severity,
        string noun,
        List<string> parts)
    {
        var count = reported.Count(entry => entry.Diagnostic.Severity == severity);
        if (count > 0)
        {
            parts.Add($"{count} {noun}{(count == 1 ? string.Empty : "s")}");
        }
    }
}

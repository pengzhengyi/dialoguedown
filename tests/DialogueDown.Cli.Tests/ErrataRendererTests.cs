using DialogueDown.Cli.Fixing;
using DialogueDown.Diagnostics;
using Spectre.Console.Testing;
using static DialogueDown.Cli.Tests.Support.LocatedDiagnosticFactory;

namespace DialogueDown.Cli.Tests;

public sealed class ErrataRendererTests
{
    [Fact]
    public void Render_Plain_WritesEachDiagnosticSortedByPosition_WithASummary()
    {
        var console = PlainConsole();
        var diagnostics = new[]
        {
            At("DLG2001", 3, 1, "duplicate anchor", DiagnosticSeverity.Error),
            At("DLG1003", 1, 5, "two jumps"),
            At("DLG3001", 2, 1, "a note", DiagnosticSeverity.Info),
        };

        new ErrataRenderer(console).Render("scene.dialogue.md", "", diagnostics);

        var output = console.Output;
        Assert.Contains("scene.dialogue.md(1,5): warning DLG1003: two jumps", output, StringComparison.Ordinal);
        Assert.Contains("scene.dialogue.md(2,1): info DLG3001: a note", output, StringComparison.Ordinal);
        Assert.Contains("scene.dialogue.md(3,1): error DLG2001: duplicate anchor", output, StringComparison.Ordinal);
        // Sorted by position: the line-1 warning is written before the line-3 error.
        Assert.True(
            output.IndexOf("DLG1003", StringComparison.Ordinal)
            < output.IndexOf("DLG2001", StringComparison.Ordinal));
        Assert.Contains("1 error, 1 warning, 1 info", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_Plain_EscapesMarkupInAMessage()
    {
        var console = PlainConsole();

        new ErrataRenderer(console).Render(
            "s.dialogue.md",
            "",
            [At("DLG1102", 1, 1, "'[x]' is not a game call", DiagnosticSeverity.Error)]);

        // The literal brackets survive rather than being parsed as (invalid) Spectre markup.
        Assert.Contains("'[x]' is not a game call", console.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_NoDiagnostics_WritesNothing()
    {
        var console = PlainConsole();

        new ErrataRenderer(console).Render("s.dialogue.md", "", []);

        Assert.Equal(string.Empty, console.Output);
    }

    [Fact]
    public void Render_Interactive_RendersRichSourceContext()
    {
        var console = InteractiveConsole();
        var source = "Alice: say `bad`";
        var diagnostics = new[]
        {
            Over(source, 11, 16, "DLG1102", "not a game call", DiagnosticSeverity.Error), // `bad`
        };

        new ErrataRenderer(console).Render("s.dialogue.md", source, diagnostics);

        var output = console.Output;
        Assert.Contains("DLG1102", output, StringComparison.Ordinal);
        Assert.Contains("not a game call", output, StringComparison.Ordinal);
        // Errata's header shows the category and severity together, and draws the source line.
        Assert.Contains("syntax error", output, StringComparison.Ordinal);
        Assert.Contains("Alice: say", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_Interactive_RendersEverySeverityIncludingAZeroWidthSpan()
    {
        var console = InteractiveConsole();
        var source = """
            line one
            line two

            """;
        var diagnostics = new[]
        {
            Over(source, 0, 4, "DLG0001", "an error", DiagnosticSeverity.Error), // the first "line"
            Over(source, 9, 13, "DLG0002", "a warning"), // the second "line"
            Over(source, 5, 5, "DLG0003", "a note", DiagnosticSeverity.Info, DiagnosticCategory.Semantic), // before "one"
        };

        new ErrataRenderer(console).Render("s.dialogue.md", source, diagnostics);

        var output = console.Output;
        // A source line proves the rich path ran (the one-liner fallback never prints source text).
        Assert.Contains("line one", output, StringComparison.Ordinal);
        Assert.Contains("DLG0001", output, StringComparison.Ordinal);
        Assert.Contains("DLG0002", output, StringComparison.Ordinal);
        Assert.Contains("DLG0003", output, StringComparison.Ordinal);
        Assert.Contains("1 error, 1 warning, 1 info", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_Plain_FollowsEachDiagnosticWithItsDocLink()
    {
        var console = PlainConsole();
        var diagnostics = new[]
        {
            At("DLG2001", 3, 1, "duplicate anchor", DiagnosticSeverity.Error),
            At("DLG1003", 1, 5, "two jumps"),
        };

        new ErrataRenderer(console).Render("scene.dialogue.md", "", diagnostics);

        var output = console.Output;
        // Each diagnostic is followed, inline, by a doc link to its own code.
        Assert.Contains(
            "for more information, see "
            + "https://pengzhengyi.github.io/dialoguedown/guide/error-codes.html#dlg1003",
            output,
            StringComparison.Ordinal);
        Assert.Contains(
            "for more information, see "
            + "https://pengzhengyi.github.io/dialoguedown/guide/error-codes.html#dlg2001",
            output,
            StringComparison.Ordinal);
        // The link sits directly under its diagnostic line, not batched at the end.
        Assert.True(
            output.IndexOf("#dlg1003", StringComparison.Ordinal)
            < output.IndexOf("DLG2001", StringComparison.Ordinal));
    }

    [Fact]
    public void Render_RepeatedCode_LinksEachOccurrenceInline()
    {
        var console = PlainConsole();
        var diagnostics = new[]
        {
            At("DLG2001", 3, 1, "duplicate anchor", DiagnosticSeverity.Error),
            At("DLG2001", 5, 1, "duplicate anchor again", DiagnosticSeverity.Error),
        };

        new ErrataRenderer(console).Render("scene.dialogue.md", "", diagnostics);

        // Each occurrence gets its own link, so the code's link appears twice.
        Assert.Equal(2, CountOccurrences(console.Output, "#dlg2001"));
    }

    [Fact]
    public void Render_Interactive_AttachesTheDocLinkToEachDiagnosticBlock()
    {
        var console = InteractiveConsole();

        new ErrataRenderer(console).Render(
            "s.dialogue.md",
            "Alice: say `bad`",
            [At("DLG1102", 1, 12, "not a game call", DiagnosticSeverity.Error)]);

        var output = console.Output;
        Assert.Contains("for more information, see", output, StringComparison.Ordinal);
        Assert.Contains("error-codes.html#dlg1102", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_Plain_HintsHowManyDiagnosticsRemainFixable()
    {
        var console = PlainConsole();

        new ErrataRenderer(console).Render(
            "s.dialogue.md",
            "",
            [
                Fixable("DLG1113", "dangling arrow", 1, 5),
                At("DLG1107", 2, 1, "styled prefix"),
            ]);

        Assert.Contains("2 warnings", console.Output, StringComparison.Ordinal);
        Assert.Contains("1 fixable with --fix", console.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_Plain_OmitsTheHintWhenNoDiagnosticCarriesAFix()
    {
        var console = PlainConsole();

        new ErrataRenderer(console).Render(
            "s.dialogue.md",
            "",
            [At("DLG1107", 1, 1, "styled prefix")]);

        Assert.DoesNotContain("fixable with --fix", console.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_FixRun_KeepsTheDiagnosticsAsFoundThenShowsTheFixSection()
    {
        var console = PlainConsole();
        const string Source = "say => now\n";
        var fix = Escape(4);
        var diagnostic = At("DLG1113", 1, 5, "dangling arrow") with
        {
            Fixes = [fix],
        };

        new ErrataRenderer(console).Render(
            "s.dialogue.md",
            Source,
            [diagnostic],
            new FixRun(
                [FixOutcome.Apply(fix)],
                WrittenFile: "s.dialogue.md",
                Remaining: [At("DLG1107", 2, 1, "styled prefix")],
                NewAfterFixing: [],
                CorrectedSource: "say \\=> now\n"));

        var output = console.Output;
        Assert.Contains("s.dialogue.md(1,5): warning DLG1113: dangling arrow", output, StringComparison.Ordinal);
        Assert.Contains("1 warning", output, StringComparison.Ordinal);
        Assert.Contains("1 fixable with --fix", output, StringComparison.Ordinal);
        Assert.Contains("Fixed s.dialogue.md (1 fix; 1 warning remains)", output, StringComparison.Ordinal);
        Assert.Contains("1. Applied Fix: Escape as literal text", output, StringComparison.Ordinal);
        Assert.Contains("-say => now", output, StringComparison.Ordinal);
        Assert.Contains("+say \\=> now", output, StringComparison.Ordinal);
        // The fix outcome is reported in the fix section, not under the diagnostic.
        Assert.DoesNotContain("  fix applied", output, StringComparison.Ordinal);
        // Diagnostics as found, then the fix section.
        Assert.True(
            output.IndexOf("for more information", StringComparison.Ordinal)
            < output.IndexOf("Fixed s.dialogue.md", StringComparison.Ordinal));
    }

    [Fact]
    public void Render_FixRun_WithNothingRemaining_OmitsTheRemainingClause()
    {
        var console = PlainConsole();
        const string Source = "say => now\n";
        var fix = Escape(4);
        var diagnostic = At("DLG1113", 1, 5, "dangling arrow") with
        {
            Fixes = [fix],
        };

        new ErrataRenderer(console).Render(
            "s.dialogue.md",
            Source,
            [diagnostic],
            new FixRun(
                [FixOutcome.Apply(fix)],
                WrittenFile: "s.dialogue.md",
                Remaining: [],
                NewAfterFixing: [],
                CorrectedSource: "say \\=> now\n"));

        Assert.Contains("Fixed s.dialogue.md (1 fix)", console.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("remains", console.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_FixRun_ShowsASkippedFixWithoutAWriteNotice()
    {
        var console = PlainConsole();
        const string Source = "say => now\n";
        var fix = Escape(4);
        var diagnostic = At("DLG1113", 1, 5, "dangling arrow") with
        {
            Fixes = [fix],
        };

        new ErrataRenderer(console).Render(
            "s.dialogue.md",
            Source,
            [diagnostic],
            new FixRun(
                [FixOutcome.Skip(fix, FixSkipReason.OverlapsAnAppliedFix)],
                WrittenFile: null,
                Remaining: [diagnostic],
                NewAfterFixing: [],
                CorrectedSource: Source));

        var output = console.Output;
        Assert.Contains(
            "1. Skipped Fix: Escape as literal text (overlaps an applied fix)",
            output,
            StringComparison.Ordinal);
        Assert.DoesNotContain("Fixed ", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_FixRun_PluralizesTheNoticeAndTheRemainingClause()
    {
        var console = PlainConsole();
        const string Source = "say => now\n";
        var first = Escape(4);
        var second = Fix("Mark the line", Insert(0, "# "));
        var diagnostic = At("DLG1113", 1, 5, "dangling arrow") with
        {
            Fixes = [first, second],
        };

        new ErrataRenderer(console).Render(
            "s.dialogue.md",
            Source,
            [diagnostic],
            new FixRun(
                [FixOutcome.Apply(first), FixOutcome.Apply(second)],
                WrittenFile: "s.dialogue.md",
                Remaining:
                [
                    At("DLG1107", 2, 1, "styled prefix"),
                    At("DLG2001", 3, 1, "duplicate anchor", DiagnosticSeverity.Error),
                ],
                NewAfterFixing: [],
                CorrectedSource: "# say \\=> now\n"));

        Assert.Contains(
            "Fixed s.dialogue.md (2 fixes; 1 error, 1 warning remain)",
            console.Output,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Render_FixRun_ReportsWhatAppearedOnlyAfterFixing()
    {
        var console = PlainConsole();
        const string Source = "say => now\n";
        var fix = Escape(4);
        var diagnostic = At("DLG1113", 1, 5, "dangling arrow") with
        {
            Fixes = [fix],
        };

        new ErrataRenderer(console).Render(
            "s.dialogue.md",
            Source,
            [diagnostic],
            new FixRun(
                [FixOutcome.Apply(fix)],
                WrittenFile: "s.dialogue.md",
                Remaining: [At("DLG2001", 2, 1, "duplicate anchor", DiagnosticSeverity.Error)],
                NewAfterFixing: [At("DLG2001", 2, 1, "duplicate anchor", DiagnosticSeverity.Error)],
                CorrectedSource: "say \\=> now\n"));

        var output = console.Output;
        Assert.Contains("after fixing:", output, StringComparison.Ordinal);
        Assert.Contains("DLG2001", output, StringComparison.Ordinal);
        Assert.True(
            output.IndexOf("Fixed s.dialogue.md", StringComparison.Ordinal)
            < output.IndexOf("after fixing:", StringComparison.Ordinal));
    }

    [Fact]
    public void Render_Interactive_ShowsTheFixSectionUnderTheRichBlocks()
    {
        var console = InteractiveConsole();
        const string Source = "Alice: say => now\n";
        var fix = Escape(11);
        var diagnostic = At("DLG1113", 1, 12, "dangling arrow") with
        {
            Fixes = [fix],
        };

        new ErrataRenderer(console).Render(
            "s.dialogue.md",
            Source,
            [diagnostic],
            new FixRun(
                [FixOutcome.Apply(fix)],
                WrittenFile: "s.dialogue.md",
                Remaining: [],
                NewAfterFixing: [],
                CorrectedSource: "Alice: say \\=> now\n"));

        var output = console.Output;
        Assert.Contains("Alice: say", output, StringComparison.Ordinal); // the rich path ran
        Assert.Contains("1. Applied Fix: Escape as literal text", output, StringComparison.Ordinal);
        Assert.Contains("│ -Alice: say => now", output, StringComparison.Ordinal);
        Assert.Contains("│ +Alice: say \\=> now", output, StringComparison.Ordinal);
        Assert.Contains("Fixed s.dialogue.md (1 fix)", output, StringComparison.Ordinal);
    }

    private static LocatedDiagnostic Fixable(string code, string message, int line, int column) =>
        At(code, line, column, message) with { Fixes = [Escape(0)] };

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        for (var index = haystack.IndexOf(needle, StringComparison.Ordinal);
            index >= 0;
            index = haystack.IndexOf(needle, index + needle.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }

    private static TestConsole PlainConsole()
    {
        var console = new TestConsole();
        console.Profile.Width = 300; // avoid wrapping so full lines can be asserted
        console.Profile.Capabilities.Interactive = false;
        return console;
    }

    private static TestConsole InteractiveConsole()
    {
        var console = new TestConsole().Interactive();
        console.Profile.Width = 300;
        return console;
    }
}

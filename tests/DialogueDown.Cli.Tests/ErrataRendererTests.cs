using DialogueDown.Cli.Fixing;
using DialogueDown.Diagnostics;
using Spectre.Console.Testing;
using static DialogueDown.Cli.Tests.Support.LocatedDiagnosticFactory;
using static DialogueDown.Cli.Tests.Support.OutputAssert;

namespace DialogueDown.Cli.Tests;

public sealed class ErrataRendererTests
{
    private const string MoreInformation =
        "for more information, see https://pengzhengyi.github.io/dialoguedown/guide/error-codes.html";

    /// <summary>A line whose arrow leads nowhere, which a compile warns about as DLG1113.</summary>
    private const string DanglingArrowScript = "say => now\n";

    /// <summary>The same line with the arrow escaped.</summary>
    private const string EscapedArrowScript = "say \\=> now\n";

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

        AssertInOrder(
            console.Output,
            "scene.dialogue.md(1,5): warning DLG1003: two jumps",
            "scene.dialogue.md(2,1): info DLG3001: a note",
            "scene.dialogue.md(3,1): error DLG2001: duplicate anchor",
            "1 error, 1 warning, 1 info");
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

        // Errata's header shows the category and severity together, and draws the source line.
        AssertMentions(console.Output, "DLG1102", "not a game call", "syntax error", "Alice: say");
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
            // A zero-width note just before "one".
            Over(source, 5, 5, "DLG0003", "a note", DiagnosticSeverity.Info, DiagnosticCategory.Semantic),
        };

        new ErrataRenderer(console).Render("s.dialogue.md", source, diagnostics);

        // A source line proves the rich path ran (the one-liner fallback never prints source text).
        AssertMentions(
            console.Output, "line one", "DLG0001", "DLG0002", "DLG0003", "1 error, 1 warning, 1 info");
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

        // Each link sits directly under its own diagnostic, not batched at the end.
        AssertInOrder(
            console.Output,
            "DLG1003",
            $"{MoreInformation}#dlg1003",
            "DLG2001",
            $"{MoreInformation}#dlg2001");
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

        AssertOccurs(console.Output, "#dlg2001", times: 2);
    }

    [Fact]
    public void Render_Interactive_AttachesTheDocLinkToEachDiagnosticBlock()
    {
        var console = InteractiveConsole();

        new ErrataRenderer(console).Render(
            "s.dialogue.md",
            "Alice: say `bad`",
            [At("DLG1102", 1, 12, "not a game call", DiagnosticSeverity.Error)]);

        AssertMentions(console.Output, "for more information, see", "error-codes.html#dlg1102");
    }

    [Fact]
    public void Render_Plain_HintsHowManyDiagnosticsRemainFixable()
    {
        var console = PlainConsole();

        new ErrataRenderer(console).Render(
            "s.dialogue.md",
            "",
            [
                At("DLG1113", 1, 5, "dangling arrow") with { Fixes = [Escape(0)] },
                At("DLG1107", 2, 1, "styled prefix"),
            ]);

        AssertMentions(console.Output, "2 warnings", "1 fixable with --fix");
    }

    [Fact]
    public void Render_Plain_OmitsTheHintWhenNoDiagnosticCarriesAFix()
    {
        var console = PlainConsole();

        new ErrataRenderer(console).Render("s.dialogue.md", "", [At("DLG1107", 1, 1, "styled prefix")]);

        Assert.DoesNotContain("fixable with --fix", console.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_FixRun_KeepsTheDiagnosticsAsFoundThenShowsTheFixSection()
    {
        var console = PlainConsole();
        var escape = EscapeTheArrow();

        new ErrataRenderer(console).Render(
            "s.dialogue.md",
            DanglingArrowScript,
            [DanglingArrowWarning(escape)],
            RunThatEscapedTheArrow(escape, remaining: [At("DLG1107", 2, 1, "styled prefix")]));

        var output = console.Output;
        AssertMentions(
            output,
            "s.dialogue.md(1,5): warning DLG1113: dangling arrow",
            "1 warning",
            "1 fixable with --fix",
            "Fixed s.dialogue.md (1 fix; 1 warning remains)",
            "1. Applied Fix: Escape as literal text",
            "-say => now",
            "+say \\=> now");
        // The fix outcome is reported in the fix section, not under the diagnostic.
        Assert.DoesNotContain("  fix applied", output, StringComparison.Ordinal);
        AssertInOrder(output, "for more information", "Fixed s.dialogue.md");
    }

    [Fact]
    public void Render_FixRun_WithNothingRemaining_OmitsTheRemainingClause()
    {
        var console = PlainConsole();
        var escape = EscapeTheArrow();

        new ErrataRenderer(console).Render(
            "s.dialogue.md",
            DanglingArrowScript,
            [DanglingArrowWarning(escape)],
            RunThatEscapedTheArrow(escape, remaining: []));

        Assert.Contains("Fixed s.dialogue.md (1 fix)", console.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("remains", console.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_FixRun_ShowsASkippedFixWithoutAWriteNotice()
    {
        var console = PlainConsole();
        var escape = EscapeTheArrow();
        var warning = DanglingArrowWarning(escape);

        new ErrataRenderer(console).Render(
            "s.dialogue.md",
            DanglingArrowScript,
            [warning],
            new FixRun(
                [FixOutcome.Skip(escape, FixSkipReason.OverlapsAnAppliedFix)],
                WrittenFile: null,
                Remaining: [warning],
                NewAfterFixing: [],
                CorrectedSource: DanglingArrowScript));

        Assert.Contains(
            "1. Skipped Fix: Escape as literal text (overlaps an applied fix)",
            console.Output,
            StringComparison.Ordinal);
        Assert.DoesNotContain("Fixed ", console.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_FixRun_PluralizesTheNoticeAndTheRemainingClause()
    {
        var console = PlainConsole();
        var escape = EscapeTheArrow();
        var markTheLine = Fix("Mark the line", Insert(0, "# "));

        new ErrataRenderer(console).Render(
            "s.dialogue.md",
            DanglingArrowScript,
            [DanglingArrowWarning(escape, markTheLine)],
            new FixRun(
                [FixOutcome.Apply(escape), FixOutcome.Apply(markTheLine)],
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
        var escape = EscapeTheArrow();
        var duplicate = At("DLG2001", 2, 1, "duplicate anchor", DiagnosticSeverity.Error);

        new ErrataRenderer(console).Render(
            "s.dialogue.md",
            DanglingArrowScript,
            [DanglingArrowWarning(escape)],
            RunThatEscapedTheArrow(escape, remaining: [duplicate], newAfterFixing: [duplicate]));

        AssertMentions(console.Output, "DLG2001");
        AssertInOrder(console.Output, "Fixed s.dialogue.md", "after fixing:");
    }

    [Fact]
    public void Render_Interactive_ShowsTheFixSectionUnderTheRichBlocks()
    {
        var console = InteractiveConsole();
        const string Source = "Alice: say => now\n";
        var fix = Escape(11);

        new ErrataRenderer(console).Render(
            "s.dialogue.md",
            Source,
            [At("DLG1113", 1, 12, "dangling arrow") with { Fixes = [fix] }],
            new FixRun(
                [FixOutcome.Apply(fix)],
                WrittenFile: "s.dialogue.md",
                Remaining: [],
                NewAfterFixing: [],
                CorrectedSource: "Alice: say \\=> now\n"));

        AssertMentions(
            console.Output,
            "Alice: say", // the rich path ran
            "1. Applied Fix: Escape as literal text",
            "│ -Alice: say => now",
            "│ +Alice: say \\=> now",
            "Fixed s.dialogue.md (1 fix)");
    }

    /// <summary>The fix that writes a backslash before the arrow in <see cref="DanglingArrowScript"/>.</summary>
    private static LocatedFix EscapeTheArrow() => Escape(4);

    /// <summary>The warning a compile reports on <see cref="DanglingArrowScript"/>.</summary>
    /// <param name="fixes">The fixes it offers, the preferred one first.</param>
    /// <returns>A DLG1113 warning at the arrow.</returns>
    private static LocatedDiagnostic DanglingArrowWarning(params LocatedFix[] fixes) =>
        At("DLG1113", 1, 5, "dangling arrow") with { Fixes = fixes };

    /// <summary>A fix run that applied <paramref name="escape"/> and wrote the script back.</summary>
    /// <param name="escape">The fix from <see cref="EscapeTheArrow"/> that the warning offered.</param>
    /// <param name="remaining">What a compile of the corrected script still reports.</param>
    /// <param name="newAfterFixing">What of that the original script did not report.</param>
    /// <returns>The run the renderer reports after the diagnostics.</returns>
    private static FixRun RunThatEscapedTheArrow(
        LocatedFix escape, LocatedDiagnostic[] remaining, LocatedDiagnostic[]? newAfterFixing = null) =>
        new(
            [FixOutcome.Apply(escape)],
            WrittenFile: "s.dialogue.md",
            Remaining: remaining,
            NewAfterFixing: newAfterFixing ?? [],
            CorrectedSource: EscapedArrowScript);

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

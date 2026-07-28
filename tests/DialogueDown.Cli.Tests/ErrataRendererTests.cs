using DialogueDown.Diagnostics;
using Spectre.Console.Testing;

namespace DialogueDown.Cli.Tests;

public sealed class ErrataRendererTests
{
    [Fact]
    public void Render_Plain_WritesEachDiagnosticSortedByPosition_WithASummary()
    {
        var console = PlainConsole();
        var diagnostics = new[]
        {
            Located("DLG2001", DiagnosticSeverity.Error, "duplicate anchor", 3, 1),
            Located("DLG1003", DiagnosticSeverity.Warning, "two jumps", 1, 5),
            Located("DLG3001", DiagnosticSeverity.Info, "a note", 2, 1),
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
            "s.dialogue.md", "", [Located("DLG1102", DiagnosticSeverity.Error, "'[x]' is not a game call", 1, 1)]);

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
        var source = "Alice: say `bad`"; // the code span `bad` at offsets 11..16 is not a game call
        var diagnostics = new[]
        {
            new LocatedDiagnostic(
                "DLG1102", DiagnosticSeverity.Error, DiagnosticCategory.Syntax, "not a game call",
                new LinePosition(1, 12), new LinePosition(1, 17), StartOffset: 11, EndOffset: 16),
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
        var source = "line one\nline two\n";
        var diagnostics = new[]
        {
            new LocatedDiagnostic(
                "DLG0001", DiagnosticSeverity.Error, DiagnosticCategory.Syntax, "an error",
                new LinePosition(1, 1), new LinePosition(1, 5), StartOffset: 0, EndOffset: 4),
            new LocatedDiagnostic(
                "DLG0002", DiagnosticSeverity.Warning, DiagnosticCategory.Syntax, "a warning",
                new LinePosition(2, 1), new LinePosition(2, 5), StartOffset: 9, EndOffset: 13),
            new LocatedDiagnostic(
                "DLG0003", DiagnosticSeverity.Info, DiagnosticCategory.Semantic, "a note",
                new LinePosition(1, 6), new LinePosition(1, 6), StartOffset: 5, EndOffset: 5), // zero-width
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
            Located("DLG2001", DiagnosticSeverity.Error, "duplicate anchor", 3, 1),
            Located("DLG1003", DiagnosticSeverity.Warning, "two jumps", 1, 5),
        };

        new ErrataRenderer(console).Render("scene.dialogue.md", "", diagnostics);

        var output = console.Output;
        // Each diagnostic is followed, inline, by a doc link to its own code (Clippy/Biome style).
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
            Located("DLG2001", DiagnosticSeverity.Error, "duplicate anchor", 3, 1),
            Located("DLG2001", DiagnosticSeverity.Error, "duplicate anchor again", 5, 1),
        };

        new ErrataRenderer(console).Render("scene.dialogue.md", "", diagnostics);

        // Inline links are per-occurrence (canonical for this style), so the code's link appears twice.
        Assert.Equal(2, CountOccurrences(console.Output, "#dlg2001"));
    }

    [Fact]
    public void Render_Interactive_AttachesTheDocLinkToEachDiagnosticBlock()
    {
        var console = InteractiveConsole();

        new ErrataRenderer(console).Render(
            "s.dialogue.md",
            "Alice: say `bad`",
            [Located("DLG1102", DiagnosticSeverity.Error, "not a game call", 1, 12)]);

        var output = console.Output;
        Assert.Contains("for more information, see", output, StringComparison.Ordinal);
        Assert.Contains("error-codes.html#dlg1102", output, StringComparison.Ordinal);
    }

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

    private static LocatedDiagnostic Located(
        string code, DiagnosticSeverity severity, string message, int line, int column) =>
        new(
            code, severity, DiagnosticCategory.Syntax, message,
            new LinePosition(line, column), new LinePosition(line, column),
            StartOffset: 0, EndOffset: 0);
}

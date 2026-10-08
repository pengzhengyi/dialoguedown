using DialogueDown.Cli.Fixing;
using DialogueDown.Diagnostics;

namespace DialogueDown.Cli.Tests;

public sealed class FixApplicationTests
{
    [Fact]
    public void NewDiagnostics_ASurvivorAfterTwoInsertions_IsNotNew()
    {
        // "a => b => [x](#missing)": both arrows are escaped, so the error two characters on is
        // the same error, now at offset 13 instead of 11.
        var application = Applied(Insert(2, "\\"), Insert(7, "\\"));
        var survivor = Diagnostic("DLG2009", 11);

        var fresh = application.NewDiagnostics([survivor], [Diagnostic("DLG2009", 13)]);

        Assert.Empty(fresh);
    }

    [Fact]
    public void NewDiagnostics_ASurvivorAfterAFixThatAddsALine_IsNotNew()
    {
        var application = Applied(Replace(0, 1, "a\nb"));

        var fresh = application.NewDiagnostics([Diagnostic("DLG2009", 5)], [Diagnostic("DLG2009", 7)]);

        Assert.Empty(fresh);
    }

    [Fact]
    public void NewDiagnostics_ASurvivorAfterADeletion_IsNotNew()
    {
        var application = Applied(Replace(0, 3, string.Empty));

        var fresh = application.NewDiagnostics([Diagnostic("DLG2009", 8)], [Diagnostic("DLG2009", 5)]);

        Assert.Empty(fresh);
    }

    [Fact]
    public void NewDiagnostics_ADiagnosticInsideTextAFixWrote_IsNew()
    {
        // The fix wrote offsets 4 and 5, so nothing there existed before.
        var application = Applied(Insert(4, "!!"));
        var inside = Diagnostic("DLG1113", 5);

        Assert.Equal([inside], application.NewDiagnostics([Diagnostic("DLG1113", 4)], [inside]));
    }

    [Fact]
    public void NewDiagnostics_ADiagnosticWhoseFixWasApplied_IsNewWhenItRemains()
    {
        // The escape was applied, so the arrow's warning should be gone; finding it again means
        // the fix did not clear it.
        var escape = Fix(Insert(4, "\\"));
        var arrow = Diagnostic("DLG1113", 4, escape);
        var application = new FixApplication("say \\=> now", [FixOutcome.Apply(escape)]);
        var remaining = Diagnostic("DLG1113", 5);

        Assert.Equal([remaining], application.NewDiagnostics([arrow], [remaining]));
    }

    [Fact]
    public void NewDiagnostics_ADiagnosticWithAnotherCode_IsNew()
    {
        var application = Applied(Insert(0, "\\"));
        var other = Diagnostic("DLG2001", 6);

        Assert.Equal([other], application.NewDiagnostics([Diagnostic("DLG2009", 5)], [other]));
    }

    [Fact]
    public void NewDiagnostics_ASkippedFix_MovesNothing()
    {
        var skipped = Fix(Insert(0, "\\"));
        var application = new FixApplication("x", [FixOutcome.Skip(skipped, FixSkipReason.OverlapsAnAppliedFix)]);

        var fresh = application.NewDiagnostics([Diagnostic("DLG2009", 5)], [Diagnostic("DLG2009", 5)]);

        Assert.Empty(fresh);
    }

    // An application whose fixes were all applied, each as one edit. Its text is not read.
    private static FixApplication Applied(params LocatedEdit[] edits) =>
        new(string.Empty, [.. edits.Select(edit => FixOutcome.Apply(Fix(edit)))]);

    private static LocatedDiagnostic Diagnostic(string code, int start, params LocatedFix[] fixes) =>
        new(
            code,
            DiagnosticSeverity.Error,
            DiagnosticCategory.Semantic,
            $"{code} problem",
            new LinePosition(1, start + 1),
            new LinePosition(1, start + 2),
            start,
            start + 1)
        {
            Fixes = fixes,
        };

    private static LocatedFix Fix(params LocatedEdit[] edits) => new("A fix", edits);

    private static LocatedEdit Insert(int at, string text) => new(at, at, text);

    private static LocatedEdit Replace(int start, int end, string text) => new(start, end, text);
}

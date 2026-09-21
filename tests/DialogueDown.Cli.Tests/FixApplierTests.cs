using DialogueDown.Diagnostics;

namespace DialogueDown.Cli.Tests;

public sealed class FixApplierTests
{
    [Fact]
    public void Apply_ADiagnosticWithOneFix_InsertsTheEscapedSigil()
    {
        const string Source = "The rule is simple => the lever opens.";
        var arrow = Source.IndexOf("=>", StringComparison.Ordinal);
        var diagnostic = Diagnostic("DLG1113", arrow, arrow + 2, Fix("Escape as literal text", Insert(arrow, "\\")));

        var application = FixApplier.Apply(Source, [diagnostic]);

        Assert.Equal("The rule is simple \\=> the lever opens.", application.Text);
        Assert.Equal(1, application.AppliedCount);
        Assert.True(application.HasCandidates);
        Assert.Null(Assert.Single(application.Reported).Fix!.SkipReason);
    }

    [Fact]
    public void Apply_ADiagnosticWithoutAFix_ReportsNoOutcomeAndKeepsTheText()
    {
        const string Source = "The rule is simple.";

        var application = FixApplier.Apply(Source, [Diagnostic("DLG1107", 0, 1)]);

        Assert.Equal(Source, application.Text);
        Assert.False(application.HasCandidates);
        Assert.Equal(0, application.AppliedCount);
        Assert.Null(Assert.Single(application.Reported).Fix);
    }

    [Fact]
    public void Apply_DiagnosticWithSeveralFixes_AppliesOnlyThePreferredOne()
    {
        const string Source = "say => now";
        var arrow = Source.IndexOf("=>", StringComparison.Ordinal);
        var preferred = Fix("Escape as literal text", Insert(arrow, "\\"));
        var alternative = Fix("Add a jump target", Replace(arrow, arrow + 2, "=> [The market](#the-market)"));
        var diagnostic = Diagnostic("DLG1113", arrow, arrow + 2, preferred, alternative);

        var application = FixApplier.Apply(Source, [diagnostic]);

        Assert.Equal("say \\=> now", application.Text);
        Assert.Equal(1, application.AppliedCount);
        Assert.Equal(preferred.Title, Assert.Single(application.Reported).Fix!.Fix.Title);
    }

    [Fact]
    public void Apply_TwoFixes_AppliesBothAgainstTheOriginalOffsets()
    {
        const string Source = "a => b => c";
        var first = Source.IndexOf("=>", StringComparison.Ordinal);
        var second = Source.IndexOf("=>", first + 2, StringComparison.Ordinal);
        var diagnostics = new[]
        {
            Diagnostic("DLG1113", first, first + 2, Fix("Escape as literal text", Insert(first, "\\"))),
            Diagnostic("DLG1113", second, second + 2, Fix("Escape as literal text", Insert(second, "\\"))),
        };

        var application = FixApplier.Apply(Source, diagnostics);

        Assert.Equal("a \\=> b \\=> c", application.Text);
        Assert.Equal(2, application.AppliedCount);
    }

    [Fact]
    public void Apply_OverlappingFixes_KeepsTheEarlierAndSkipsTheLater()
    {
        const string Source = "abcdef";
        var first = Diagnostic("DLG0001", 1, 4, Fix("first", Replace(1, 4, "X")));
        var second = Diagnostic("DLG0002", 2, 5, Fix("second", Replace(2, 5, "Y")));

        var application = FixApplier.Apply(Source, [first, second]);

        Assert.Equal("aXef", application.Text);
        Assert.Equal(1, application.AppliedCount);
        Assert.Equal(
            FixSkipReason.OverlapsAnAppliedFix,
            application.Reported[1].Fix!.SkipReason);
    }

    [Fact]
    public void Apply_TwoInsertionsAtTheSameOffset_KeepsTheOneThatSortsFirst()
    {
        const string Source = "ab";
        var later = Diagnostic("DLG0002", 1, 1, Fix("second", Insert(1, "Y")));
        var earlier = Diagnostic("DLG0001", 1, 1, Fix("first", Insert(1, "X")));

        // The compiler's emission order is not a contract, so the later code still loses.
        var application = FixApplier.Apply(Source, [later, earlier]);

        Assert.Equal("aXb", application.Text);
        Assert.Equal(1, application.AppliedCount);
        Assert.Equal(
            FixSkipReason.OverlapsAnAppliedFix,
            application.Reported[0].Fix!.SkipReason);
    }

    [Fact]
    public void Apply_AnEditOutsideTheText_SkipsThatFixAndAppliesTheRest()
    {
        const string Source = "ab";
        var outside = Diagnostic("DLG0001", 0, 0, Fix("outside", new LocatedEdit(5, 6, "X")));
        var inside = Diagnostic("DLG0002", 1, 1, Fix("inside", Insert(1, "Y")));

        var application = FixApplier.Apply(Source, [outside, inside]);

        Assert.Equal("aYb", application.Text);
        Assert.Equal(1, application.AppliedCount);
        Assert.Equal(FixSkipReason.OutsideTheScript, application.Reported[0].Fix!.SkipReason);
        Assert.Null(application.Reported[1].Fix!.SkipReason);
    }

    [Fact]
    public void Apply_AMultiEditFix_AppliesEveryEditAsOneSplice()
    {
        const string Source = "say => now";
        var arrow = Source.IndexOf("=>", StringComparison.Ordinal);
        var fix = Fix("Parenthesize", Insert(arrow, "("), Insert(arrow + 2, ")"));
        var diagnostic = Diagnostic("DLG0001", arrow, arrow + 2, fix);

        var application = FixApplier.Apply(Source, [diagnostic]);

        Assert.Equal("say (=>) now", application.Text);
        Assert.Equal(1, application.AppliedCount);
    }

    [Fact]
    public void Apply_AnInsertionAtTheTextLength_Appends()
    {
        const string Source = "ab";
        var diagnostic = Diagnostic("DLG0001", 2, 2, Fix("Append", Insert(2, "!")));

        var application = FixApplier.Apply(Source, [diagnostic]);

        Assert.Equal("ab!", application.Text);
        Assert.Equal(1, application.AppliedCount);
    }

    [Fact]
    public void Apply_KeptFixesAndSkippedFixes_KeepTheCompileDiagnosticOrder()
    {
        const string Source = "abcdef";
        var first = Diagnostic("DLG0001", 1, 4, Fix("first", Replace(1, 4, "X")));
        var second = Diagnostic("DLG0002", 2, 5, Fix("second", Replace(2, 5, "Y")));
        var third = Diagnostic("DLG0003", 5, 5, Fix("third", Insert(5, "!")));

        var application = FixApplier.Apply(Source, [first, second, third]);

        Assert.Equal(["DLG0001", "DLG0002", "DLG0003"], application.Reported.Select(r => r.Diagnostic.Code));
        Assert.Equal("aXe!f", application.Text);
    }

    private static LocatedDiagnostic Diagnostic(string code, int start, int end, params LocatedFix[] fixes) =>
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

    private static LocatedFix Fix(string title, params LocatedEdit[] edits) => new(title, edits);

    private static LocatedEdit Insert(int at, string text) => new(at, at, text);

    private static LocatedEdit Replace(int start, int end, string text) => new(start, end, text);
}

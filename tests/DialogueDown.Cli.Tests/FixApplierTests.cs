using DialogueDown.Cli.Fixing;
using static DialogueDown.Cli.Tests.Support.LocatedDiagnosticFactory;

namespace DialogueDown.Cli.Tests;

public sealed class FixApplierTests
{
    [Fact]
    public void Apply_ADiagnosticWithOneFix_InsertsTheEscapedSigil()
    {
        const string Source = "The rule is simple => the lever opens.";
        var arrow = Source.IndexOf("=>", StringComparison.Ordinal);
        var diagnostic = Spanning("DLG1113", arrow, arrow + 2, Escape(arrow));

        var application = FixApplier.Apply(Source, [diagnostic]);

        Assert.Equal("The rule is simple \\=> the lever opens.", application.Text);
        Assert.Equal(1, application.AppliedCount);
        Assert.True(application.HasCandidates);
        Assert.Null(Assert.Single(application.Outcomes).SkipReason);
    }

    [Fact]
    public void Apply_ADiagnosticWithoutAFix_ReportsNoOutcomeAndKeepsTheText()
    {
        const string Source = "The rule is simple.";

        var application = FixApplier.Apply(Source, [Spanning("DLG1107", 0, 1)]);

        Assert.Equal(Source, application.Text);
        Assert.False(application.HasCandidates);
        Assert.Equal(0, application.AppliedCount);
        Assert.Empty(application.Outcomes);
    }

    [Fact]
    public void Apply_DiagnosticWithSeveralFixes_AppliesOnlyThePreferredOne()
    {
        const string Source = "say => now";
        var arrow = Source.IndexOf("=>", StringComparison.Ordinal);
        var preferred = Escape(arrow);
        var alternative = Fix("Add a jump target", Replace(arrow, arrow + 2, "=> [The market](#the-market)"));
        var diagnostic = Spanning("DLG1113", arrow, arrow + 2, preferred, alternative);

        var application = FixApplier.Apply(Source, [diagnostic]);

        Assert.Equal("say \\=> now", application.Text);
        Assert.Equal(1, application.AppliedCount);
        Assert.Equal(preferred.Title, Assert.Single(application.Outcomes).Fix.Title);
    }

    [Fact]
    public void Apply_TwoFixes_AppliesBothAgainstTheOriginalOffsets()
    {
        const string Source = "a => b => c";
        var first = Source.IndexOf("=>", StringComparison.Ordinal);
        var second = Source.IndexOf("=>", first + 2, StringComparison.Ordinal);
        var diagnostics = new[]
        {
            Spanning("DLG1113", first, first + 2, Escape(first)),
            Spanning("DLG1113", second, second + 2, Escape(second)),
        };

        var application = FixApplier.Apply(Source, diagnostics);

        Assert.Equal("a \\=> b \\=> c", application.Text);
        Assert.Equal(2, application.AppliedCount);
    }

    [Fact]
    public void Apply_OverlappingFixes_KeepsTheEarlierAndSkipsTheLater()
    {
        const string Source = "abcdef";
        var first = Spanning("DLG0001", 1, 4, Fix("first", Replace(1, 4, "X")));
        var second = Spanning("DLG0002", 2, 5, Fix("second", Replace(2, 5, "Y")));

        var application = FixApplier.Apply(Source, [first, second]);

        Assert.Equal("aXef", application.Text);
        Assert.Equal(1, application.AppliedCount);
        Assert.Equal(
            FixSkipReason.OverlapsAnAppliedFix,
            application.Outcomes[1].SkipReason);
    }

    [Fact]
    public void Apply_TwoInsertionsAtTheSameOffset_KeepsTheOneThatSortsFirst()
    {
        const string Source = "ab";
        var later = Spanning("DLG0002", 1, 1, Fix("second", Insert(1, "Y")));
        var earlier = Spanning("DLG0001", 1, 1, Fix("first", Insert(1, "X")));

        // Passed in reverse order: the fix whose code sorts first still wins.
        var application = FixApplier.Apply(Source, [later, earlier]);

        Assert.Equal("aXb", application.Text);
        Assert.Equal(1, application.AppliedCount);
        Assert.Equal(
            FixSkipReason.OverlapsAnAppliedFix,
            application.Outcomes[1].SkipReason);
    }

    [Fact]
    public void Apply_AnEditOutsideTheText_SkipsThatFixAndAppliesTheRest()
    {
        const string Source = "ab";
        var outside = Spanning("DLG0001", 0, 0, Fix("outside", Replace(5, 6, "X")));
        var inside = Spanning("DLG0002", 1, 1, Fix("inside", Insert(1, "Y")));

        var application = FixApplier.Apply(Source, [outside, inside]);

        Assert.Equal("aYb", application.Text);
        Assert.Equal(1, application.AppliedCount);
        Assert.Equal(FixSkipReason.OutsideTheScript, application.Outcomes[1].SkipReason);
        Assert.Null(application.Outcomes[0].SkipReason);
    }

    [Fact]
    public void Apply_AMultiEditFix_AppliesEveryEditAsOneSplice()
    {
        const string Source = "say => now";
        var arrow = Source.IndexOf("=>", StringComparison.Ordinal);
        var fix = Fix("Parenthesize", Insert(arrow, "("), Insert(arrow + 2, ")"));
        var diagnostic = Spanning("DLG0001", arrow, arrow + 2, fix);

        var application = FixApplier.Apply(Source, [diagnostic]);

        Assert.Equal("say (=>) now", application.Text);
        Assert.Equal(1, application.AppliedCount);
    }

    [Fact]
    public void Apply_AnInsertionAtTheTextLength_Appends()
    {
        const string Source = "ab";
        var diagnostic = Spanning("DLG0001", 2, 2, Fix("Append", Insert(2, "!")));

        var application = FixApplier.Apply(Source, [diagnostic]);

        Assert.Equal("ab!", application.Text);
        Assert.Equal(1, application.AppliedCount);
    }

    [Fact]
    public void Apply_KeptFixesAndSkippedFixes_KeepTheCompileDiagnosticOrder()
    {
        const string Source = "abcdef";
        var first = Spanning("DLG0001", 1, 4, Fix("first", Replace(1, 4, "X")));
        var second = Spanning("DLG0002", 2, 5, Fix("second", Replace(2, 5, "Y")));
        var third = Spanning("DLG0003", 5, 5, Fix("third", Insert(5, "!")));

        var application = FixApplier.Apply(Source, [first, second, third]);

        Assert.Equal(3, application.Outcomes.Count);
        Assert.Equal([true, false, true], application.Outcomes.Select(outcome => outcome.Applied));
        Assert.Equal("aXe!f", application.Text);
    }
}

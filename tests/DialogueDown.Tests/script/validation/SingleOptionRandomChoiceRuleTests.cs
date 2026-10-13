using DialogueDown.Common;
using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Validation;
using static DialogueDown.Tests.Support.DiagnosticsAssert;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Validation;

public sealed class SingleOptionRandomChoiceRuleTests
{
    [Fact]
    public void Check_ASingleOptionRandomChoice_WarnsAtTheGroupStart()
    {
        var only = RandomOption(NumberWeight(50), Line(Text("only")));
        var random = RandomChoices(only) with { Span = new SourceSpan(9, 1) };

        var diagnostic = AssertReported(Check(random), DiagnosticCatalog.SingleOptionRandomChoice);

        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal(SourceSpan.EmptyAt(9), diagnostic.Span);
    }

    [Fact]
    public void Check_ASingleZeroWeightOption_IsLeftToTheZeroTotalError()
    {
        // The option is never selected, which the zero-total error says; a warning that it is
        // always selected would contradict it.
        var random = RandomChoices(RandomOption(NumberWeight(0), Line(Text("never"))));

        Assert.Empty(Check(random));
    }

    [Fact]
    public void Check_AGuardedSingleZeroWeightOption_StillWarns()
    {
        // The zero-total error leaves a guarded random choice to the runtime, so this warning is
        // the one thing that points at it.
        var random = RandomChoices(
            ConditionalRandomOption(Condition("IsAngry"), NumberWeight(0), Line(Text("never"))));

        AssertReported(Check(random), DiagnosticCatalog.SingleOptionRandomChoice);
    }

    [Fact]
    public void Check_MultipleOptions_ReportsNothing()
    {
        var random = RandomChoices(
            RandomOption(NumberWeight(50), Line(Text("a"))),
            RandomOption(NumberWeight(50), Line(Text("b"))));

        Assert.Empty(Check(random));
    }

    private static IReadOnlyList<Diagnostic> Check(RandomChoices root)
    {
        var bag = new DiagnosticBag();
        var document = DesugaredDocument(root);
        new SingleOptionRandomChoiceRule().Check(DialogueTreeIndex.Build(document), bag);
        return bag.Diagnostics;
    }
}


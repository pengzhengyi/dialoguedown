using DialogueDown.Common;
using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Validation;
using DialogueDown.Script.Weights;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Validation;

public sealed class WeightTotalRuleTests
{
    [Fact]
    public void Check_WeightsTotalOneHundred_ReportsNothing()
    {
        var random = RandomChoices(
            RandomOption(NumberWeight(50), Line(Text("heads"))),
            RandomOption(NumberWeight(50), Line(Text("tails"))));

        Assert.Empty(Check(random));
    }

    [Fact]
    public void Check_WeightsBelowOneHundred_WarnsAtTheGroupStart_WithTheTotal()
    {
        var random = RandomChoices(
            12,
            RandomOption(NumberWeight(50), Line(Text("heads"))),
            RandomOption(NumberWeight(30), Line(Text("tails"))));

        var diagnostic = Assert.Single(Check(random));

        Assert.Equal(DiagnosticCatalog.ChoiceWeightsNotOneHundred.Code, diagnostic.Descriptor.Code);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal(SourceSpan.EmptyAt(12), diagnostic.Span);
        Assert.Equal(["80"], diagnostic.MessageArguments);
    }

    [Fact]
    public void Check_WeightsAboveOneHundred_Warns()
    {
        var random = RandomChoices(
            RandomOption(NumberWeight(60), Line(Text("heads"))),
            RandomOption(NumberWeight(60), Line(Text("tails"))));

        var diagnostic = Assert.Single(Check(random));

        Assert.Equal(DiagnosticCatalog.ChoiceWeightsNotOneHundred.Code, diagnostic.Descriptor.Code);
        Assert.Equal(["120"], diagnostic.MessageArguments);
    }

    [Fact]
    public void Check_WeightsSumToZero_ReportsAnError()
    {
        var random = RandomChoices(
            7,
            RandomOption(NumberWeight(0), Line(Text("heads"))),
            RandomOption(NumberWeight(0), Line(Text("tails"))));

        var diagnostic = Assert.Single(Check(random));

        Assert.Equal(DiagnosticCatalog.ZeroChoiceWeightTotal.Code, diagnostic.Descriptor.Code);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal(SourceSpan.EmptyAt(7), diagnostic.Span);
    }

    [Fact]
    public void Check_AnAutoWeightThatFillsTheLeftover_ReportsNothing()
    {
        var random = RandomChoices(
            RandomOption(NumberWeight(70), Line(Text("halt"))),
            RandomOption(AutoWeight(), Line(Text("oh, it's you"))));

        Assert.Empty(Check(random));
    }

    [Fact]
    public void Check_ASingleOption_IsAlwaysSelected_SoReportsNothing()
    {
        var random = RandomChoices(RandomOption(NumberWeight(50), Line(Text("only"))));

        Assert.Empty(Check(random));
    }

    [Fact]
    public void Check_WeightsCloseEnoughToOneHundred_ReportNothing()
    {
        var random = RandomChoices(
            RandomOption(NumberWeight(33.3), Line(Text("a"))),
            RandomOption(NumberWeight(33.3), Line(Text("b"))),
            RandomOption(NumberWeight(33.3), Line(Text("c"))));

        Assert.Empty(Check(random));
    }

    [Fact]
    public void Check_AQueryWeightInTheGroup_DefersTheChecksToRuntime()
    {
        var random = RandomChoices(
            RandomOption(NumberWeight(20), Line(Text("static"))),
            RandomOption(QueryWeight("Bob.Affection"), Line(Text("dynamic"))));

        Assert.Empty(Check(random));
    }

    private static IReadOnlyList<Diagnostic> Check(RandomChoices root)
    {
        var bag = new DiagnosticBag();
        var document = new DesugaredScriptDocument(new ScriptDocument([root]));
        new WeightTotalRule(new DefaultWeightNormalization())
            .Check(DialogueTreeIndex.Build(document), bag);
        return bag.Diagnostics;
    }
}

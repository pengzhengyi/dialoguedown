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
        var random = RandomChoices(9, RandomOption(new NumberWeight(50), Line(Text("only"))));

        var diagnostic = AssertReported(Check(random), DiagnosticCatalog.SingleOptionRandomChoice);

        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal(SourceSpan.EmptyAt(9), diagnostic.Span);
    }

    [Fact]
    public void Check_ASingleZeroWeightOption_AlsoWarns()
    {
        var random = RandomChoices(RandomOption(new NumberWeight(0), Line(Text("never?"))));

        AssertReported(Check(random), DiagnosticCatalog.SingleOptionRandomChoice);
    }

    [Fact]
    public void Check_MultipleOptions_ReportsNothing()
    {
        var random = RandomChoices(
            RandomOption(new NumberWeight(50), Line(Text("a"))),
            RandomOption(new NumberWeight(50), Line(Text("b"))));

        Assert.Empty(Check(random));
    }

    private static IReadOnlyList<Diagnostic> Check(RandomChoices root)
    {
        var bag = new DiagnosticBag();
        var document = new DesugaredScriptDocument(new ScriptDocument([root]));
        new SingleOptionRandomChoiceRule().Check(DialogueTreeIndex.Build(document), bag);
        return bag.Diagnostics;
    }
}


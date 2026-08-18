using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Validation;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DiagnosticsAssert;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Validation;

public sealed class OrphanConditionRuleTests
{
    [Fact]
    public void Check_AConditionInSpeech_ReportsAtTheConditionSpan()
    {
        var condition = new Condition("Rainy", SourceSpanFactory.Span(7));
        var line = Line(condition);

        var diagnostic = AssertReported(Check(line), DiagnosticCatalog.OrphanCondition);

        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal(condition.Span, diagnostic.Span);
    }

    [Fact]
    public void Check_AConditionBoundToAJump_ReportsNothing()
    {
        var jump = new Jump("#play", [], SourceSpanFactory.Span(), Condition("Rainy"));
        var line = Line(jump);

        Assert.Empty(Check(line));
    }

    [Fact]
    public void Check_AConditionBoundToALine_ReportsNothing()
    {
        var line = ConditionalLine(Condition("Rainy"), Text("The moor is bleak."));

        Assert.Empty(Check(line));
    }

    [Fact]
    public void Check_ALineConditionBesideAStrayCondition_ReportsOnlyTheStrayOne()
    {
        var stray = new Condition("Sunny", SourceSpanFactory.Span(20));
        var line = ConditionalLine(Condition("Rainy"), Text("It is "), stray);

        var diagnostics = Check(line);
        var diagnostic = AssertReported(diagnostics, DiagnosticCatalog.OrphanCondition);
        Assert.Equal(stray.Span, diagnostic.Span);
        Assert.Single(diagnostics);
    }

    [Fact]
    public void Check_AConditionBoundToAChoice_ReportsNothing()
    {
        var choices = Choices(ConditionalChoice(Condition("Rainy"), Line(Text("Use it."))));

        Assert.Empty(Check(choices));
    }

    [Fact]
    public void Check_AConditionBoundToARandomOption_ReportsNothing()
    {
        var random = RandomChoices(
            ConditionalRandomOption(Condition("Rainy"), NumberWeight(50), Line(Text("maybe"))),
            RandomOption(NumberWeight(50), Line(Text("always"))));

        Assert.Empty(Check(random));
    }

    [Fact]
    public void Check_AConditionBoundToAControlLine_ReportsNothing()
    {
        var control = ConditionalControlLine(
            Condition("GateJammed"), new DefaultCommand("force", SourceSpanFactory.Span()));

        Assert.Empty(Check(control));
    }

    [Fact]
    public void Check_AConditionBoundToAControlBranch_ReportsNothing()
    {
        var control = ControlBlock(
            Branch(Condition("Rainy"), Line(Text("The moor is bleak."))),
            ElseBranch(Line(Text("The sky is clear."))));

        Assert.Empty(Check(control));
    }

    private static IReadOnlyList<Diagnostic> Check(ScriptBlock root)
    {
        var bag = new DiagnosticBag();
        var document = new DesugaredScriptDocument(new ScriptDocument([root]));
        new OrphanConditionRule().Check(DialogueTreeIndex.Build(document), bag);
        return bag.Diagnostics;
    }
}

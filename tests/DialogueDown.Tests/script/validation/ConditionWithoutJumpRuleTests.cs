using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Validation;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DiagnosticsAssert;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Validation;

public sealed class ConditionWithoutJumpRuleTests
{
    [Fact]
    public void Check_AConditionNotBeforeAJump_ReportsAtTheConditionSpan()
    {
        var condition = new Condition("Rainy", SourceSpanFactory.Span(7));
        var line = Line(condition);

        var diagnostic = AssertReported(Check(line), DiagnosticCatalog.ConditionWithoutJump);

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

    private static IReadOnlyList<Diagnostic> Check(ScriptBlock root)
    {
        var bag = new DiagnosticBag();
        var document = new DesugaredScriptDocument(new ScriptDocument([root]));
        new ConditionWithoutJumpRule().Check(DialogueTreeIndex.Build(document), bag);
        return bag.Diagnostics;
    }
}

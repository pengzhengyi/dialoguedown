using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Validation;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Validation;

public sealed class MultipleJumpsOnLineRuleTests
{
    private readonly MultipleJumpsOnLineRule _rule = new();

    [Fact]
    public void Check_LineWithTwoJumps_ReportsAWarningAtTheLineWithTheCount()
    {
        var line = Line(Jump("#a"), Text(" or "), Jump("#b"));

        var diagnostic = Assert.Single(Check(line));

        Assert.Equal(DiagnosticCatalog.MultipleJumpsOnLine.Code, diagnostic.Descriptor.Code);
        Assert.Equal(DiagnosticCategory.Syntax, diagnostic.Descriptor.Category);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal(line.Span, diagnostic.Span);
        Assert.Equal([2], diagnostic.MessageArguments);
    }

    [Fact]
    public void Check_LineWithThreeJumps_ReportsTheCountThree()
    {
        var diagnostics = Check(Line(Jump("#a"), Jump("#b"), Jump("#c")));

        Assert.Equal([3], Assert.Single(diagnostics).MessageArguments);
    }

    [Fact]
    public void Check_LineWithOneJump_ReportsNothing()
    {
        Assert.Empty(Check(Line(Jump("#a"))));
    }

    [Fact]
    public void Check_LineWithNoJumps_ReportsNothing()
    {
        Assert.Empty(Check(Line(Text("just talking"))));
    }

    [Fact]
    public void Check_JumpsNestedInAChoice_AreChecked()
    {
        var line = Line(Jump("#a"), Jump("#b"));

        var diagnostic = Assert.Single(Check(Choices(Choice(line))));

        Assert.Equal(line.Span, diagnostic.Span);
    }

    private IReadOnlyList<Diagnostic> Check(params ScriptBlock[] blocks)
    {
        var bag = new DiagnosticBag();
        var index = DialogueTreeIndex.Build(new DesugaredScriptDocument(new ScriptDocument(blocks)));
        _rule.Check(index, bag);
        return bag.Diagnostics;
    }
}

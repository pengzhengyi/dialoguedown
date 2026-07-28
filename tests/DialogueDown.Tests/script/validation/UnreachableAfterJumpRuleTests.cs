using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Validation;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Validation;

public sealed class UnreachableAfterJumpRuleTests
{
    private readonly UnreachableAfterJumpRule _rule = new();

    [Fact]
    public void Check_TextAfterAJump_ReportsAWarning()
    {
        var line = Line(Jump("#a"), Text(" and then more"));

        var diagnostic = Assert.Single(Check(line));

        Assert.Equal(DiagnosticCatalog.UnreachableContentAfterJump.Code, diagnostic.Descriptor.Code);
        Assert.Equal(DiagnosticCategory.Syntax, diagnostic.Descriptor.Category);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public void Check_ASecondJumpAfterAJump_IsUnreachable()
    {
        // A second jump is just unreachable content after the first — the same finding.
        Assert.Single(Check(Line(Jump("#a"), Text(" or "), Jump("#b"))));
    }

    [Fact]
    public void Check_ReportsOncePerLine_EvenWithSeveralTrailingFragments()
    {
        Assert.Single(Check(Line(Jump("#a"), Jump("#b"), Jump("#c"))));
    }

    [Fact]
    public void Check_AJumpAsTheLastContent_ReportsNothing()
    {
        Assert.Empty(Check(Line(Text("Bye now "), Jump("#a"))));
    }

    [Fact]
    public void Check_OnlyBlankTextAfterAJump_ReportsNothing()
    {
        Assert.Empty(Check(Line(Jump("#a"), Text("   "))));
    }

    [Fact]
    public void Check_ALineWithNoJump_ReportsNothing()
    {
        Assert.Empty(Check(Line(Text("just talking"))));
    }

    [Fact]
    public void Check_TrailingContentNestedInAChoice_IsChecked()
    {
        var line = Line(Jump("#a"), Text(" more"));

        Assert.Single(Check(Choices(Choice(line))));
    }

    [Fact]
    public void Check_ContentAfterAJumpInAControlLine_IsChecked()
    {
        // A bare jump followed by another effect is a control line; the unreachable effect counts.
        Assert.Single(Check(ControlLine(Jump("#a"), Jump("#b"))));
    }

    private IReadOnlyList<Diagnostic> Check(params ScriptBlock[] blocks)
    {
        var bag = new DiagnosticBag();
        var index = DialogueTreeIndex.Build(new DesugaredScriptDocument(new ScriptDocument(blocks)));
        _rule.Check(index, bag);
        return bag.Diagnostics;
    }
}

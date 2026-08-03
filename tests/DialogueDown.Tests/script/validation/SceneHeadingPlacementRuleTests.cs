using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Validation;
using static DialogueDown.Tests.Support.DiagnosticsAssert;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Validation;

public sealed class SceneHeadingPlacementRuleTests
{
    [Fact]
    public void Check_HeadingInsideControlBranch_ReportsAtTheHeadingSpan()
    {
        var heading = SceneHeading("Nested scene");
        var control = ControlBlock(Branch(Condition("Rainy"), heading));

        var diagnostic = AssertReported(Check(control), "DLG2015");

        Assert.Equal(heading.Span, diagnostic.Span);
    }

    [Fact]
    public void Check_HeadingInsidePlayerChoice_Reports()
    {
        var choices = Choices(Choice(SceneHeading("Nested scene")));

        AssertReported(Check(choices), "DLG2015");
    }

    [Fact]
    public void Check_HeadingInsideRandomOption_Reports()
    {
        var random = RandomChoices(RandomOption(AutoWeight(), SceneHeading("Nested scene")));

        AssertReported(Check(random), "DLG2015");
    }

    [Fact]
    public void Check_TopLevelHeading_ReportsNothing() =>
        Assert.Empty(Check(SceneHeading("Top-level scene")));

    private static IReadOnlyList<Diagnostic> Check(ScriptBlock root)
    {
        var bag = new DiagnosticBag();
        var document = new DesugaredScriptDocument(new ScriptDocument([root]));
        new SceneHeadingPlacementRule().Check(DialogueTreeIndex.Build(document), bag);
        return bag.Diagnostics;
    }
}

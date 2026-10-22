using DialogueDown.Common;
using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Validation;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Validation;

public sealed class ChoiceNestingDepthRuleTests
{
    [Fact]
    public void Check_FourthChoiceLevel_ReportsStyleWarningAtGroupStart()
    {
        var level4 = Choices(Choice(Line(Text("deep")))) with { Span = new SourceSpan(40, 1) };
        var root = NestToFourthLevel(level4);

        var diagnostic = Assert.Single(Check(root));

        Assert.Equal(DiagnosticCatalog.DeeplyNestedChoiceBranch.Code, diagnostic.Descriptor.Code);
        Assert.Equal(DiagnosticCategory.Style, diagnostic.Descriptor.Category);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal(SourceSpan.EmptyAt(40), diagnostic.Span);
        Assert.Equal([4, 3], diagnostic.MessageArguments);
    }

    [Fact]
    public void Check_ThirdChoiceLevel_ReportsNothing()
    {
        var level3 = Choices(Choice(Line(Text("allowed"))));
        var level2 = Choices(Choice(level3));
        var root = Choices(Choice(level2));

        var diagnostics = Check(root);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Check_DeeperDescendants_ReportsOnlyFirstOverLimitGroup()
    {
        var level5 = Choices(Choice(Line(Text("deeper")))) with { Span = new SourceSpan(50, 1) };
        var level4 = Choices(Choice(level5)) with { Span = new SourceSpan(40, 1) };
        var root = NestToFourthLevel(level4);

        var diagnostic = Assert.Single(Check(root));

        Assert.Equal(SourceSpan.EmptyAt(40), diagnostic.Span);
    }

    [Fact]
    public void Check_SeparateOverNestedBranches_ReportsEachBranch()
    {
        var left = NestFromSecondLevel(Choices(Choice(Line(Text("left")))) with { Span = new SourceSpan(40, 1) });
        var right = NestFromSecondLevel(Choices(Choice(Line(Text("right")))) with { Span = new SourceSpan(80, 1) });
        var root = Choices(Choice(left), Choice(right));

        var diagnostics = Check(root);

        Assert.Equal(
            [SourceSpan.EmptyAt(40), SourceSpan.EmptyAt(80)],
            diagnostics.Select(diagnostic => diagnostic.Span));
    }

    [Fact]
    public void Check_OrderedChoiceGroupAtFourthLevel_Reports()
    {
        var level4 = Choices(isOrdered: true, Choice(Line(Text("ordered"))));
        var root = NestToFourthLevel(level4);

        var diagnostic = Assert.Single(Check(root));

        Assert.Equal(DiagnosticCatalog.DeeplyNestedChoiceBranch.Code, diagnostic.Descriptor.Code);
    }

    [Fact]
    public void Check_CustomMaximum_UsesConfiguredDepth()
    {
        var level2 = Choices(Choice(Line(Text("deep")))) with { Span = new SourceSpan(20, 1) };
        var root = Choices(Choice(level2));

        var diagnostic = Assert.Single(Check(root, maximumNestingLevel: 1));

        Assert.Equal(SourceSpan.EmptyAt(20), diagnostic.Span);
        Assert.Equal([2, 1], diagnostic.MessageArguments);
    }

    [Fact]
    public void Check_RandomChoicesCountTowardNestingDepth()
    {
        // Depth mixes both group kinds: Choices > RandomChoices > Choices > RandomChoices.
        var deep = RandomOption(NumberWeight(50), Line(Text("deep")));
        var level4 = RandomChoices(deep) with { Span = new SourceSpan(40, 1) };
        var level3 = Choices(Choice(level4));
        var level2 = RandomChoices(RandomOption(AutoWeight(), level3));
        var root = Choices(Choice(level2));

        var diagnostic = Assert.Single(Check(root));

        Assert.Equal(DiagnosticCatalog.DeeplyNestedChoiceBranch.Code, diagnostic.Descriptor.Code);
        Assert.Equal(SourceSpan.EmptyAt(40), diagnostic.Span);
        Assert.Equal([4, 3], diagnostic.MessageArguments);
    }

    [Fact]
    public void Constructor_NonPositiveMaximum_Throws()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new ChoiceNestingDepthRule(0));

        Assert.Equal("maximumNestingLevel", exception.ParamName);
    }

    /// <summary>
    /// Three menus, each the only option of the one around it, holding the group given at the
    /// fourth level.
    /// </summary>
    /// <remarks>
    /// The options between hold nothing but the next menu, a shape no script writes; the rule reads
    /// only how deep a group sits.
    /// </remarks>
    /// <param name="level4">The group that sits at the fourth level.</param>
    /// <returns>The outermost menu.</returns>
    private static Choices NestToFourthLevel(Choices level4)
    {
        var level3 = Choices(Choice(level4));
        return Choices(Choice(Choices(Choice(level3))));
    }

    /// <summary>
    /// Two menus, one inside the other, around the group given: placed as an option of a top-level
    /// menu, the group sits at the fourth level.
    /// </summary>
    /// <remarks>
    /// As in <see cref="NestToFourthLevel"/>, the options between hold only the next menu.
    /// </remarks>
    /// <param name="level4">The group that sits at the fourth level once this is nested once more.</param>
    /// <returns>The menu at the second level.</returns>
    private static Choices NestFromSecondLevel(Choices level4)
    {
        var level3 = Choices(Choice(level4));
        return Choices(Choice(level3));
    }

    private static IReadOnlyList<Diagnostic> Check(Choices root, int maximumNestingLevel = 3)
    {
        var bag = new DiagnosticBag();
        var document = DesugaredDocument(root);
        new ChoiceNestingDepthRule(maximumNestingLevel)
            .Check(DialogueTreeIndex.Build(document), bag);
        return bag.Diagnostics;
    }
}

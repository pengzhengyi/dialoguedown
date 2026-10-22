using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Validation;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DiagnosticsAssert;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Validation;

public sealed class KeyNeededBothWaysRuleTests
{
    [Fact]
    public void Check_ALinesConditionKeyQueriedInItsSpeech_ReportsAnErrorAtTheQuery()
    {
        var query = Query("Hero.Title") with { Span = SourceSpanFactory.Span(31, 14) };

        var diagnostic = AssertReported(Check(GuardedAndQueryingOneKey(query)), DiagnosticCatalog.KeyNeededBothWays);

        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal(query.Span, diagnostic.Span);
    }

    [Fact]
    public void Check_ALinesConditionKeyQueriedInItsSpeech_NamesTheKeyTheLineAndBothUses()
    {
        var diagnostic = AssertReported(
            Check(GuardedAndQueryingOneKey(Query("Hero.Title"))), DiagnosticCatalog.KeyNeededBothWays);

        Assert.Equal(["Hero.Title", "a query", "line", "a condition"], diagnostic.MessageArguments);
    }

    [Fact]
    public void Check_AQueryInALinkInTheLine_Reports() =>
        AssertReported(Check(GuardedAndQueryingOneKeyInALink()), DiagnosticCatalog.KeyNeededBothWays);

    [Fact]
    public void Check_AQueryInTheJumpEndingTheLine_ReportsNothing() =>
        AssertNotReported(Check(GuardedAndQueryingOneKeyInItsJump()));

    [Fact]
    public void Check_AJumpsConditionKeyQueriedInTheLine_ReportsNothing() =>
        AssertNotReported(Check(QueryingTheKeyItsJumpIsGuardedBy()));

    [Fact]
    public void Check_ALinesConditionAndQueryOnDifferentKeys_ReportsNothing() =>
        AssertNotReported(Check(ConditionalLine(Condition("Hero.HasTitle"), Text("Welcome, "), Query("Hero.Title"))));

    [Fact]
    public void Check_OneKeyGuardingALineAndQueriedInTheNext_ReportsNothing() =>
        AssertNotReported(Check(
            ConditionalLine(Condition("Hero.Title"), Text("Halt.")),
            Line(Text("Welcome, "), Query("Hero.Title"))));

    [Fact]
    public void Check_AnOptionsConditionKeyQueriedInAnotherOptionsWords_ReportsAtTheQuery()
    {
        var query = Query("Hero.Title") with { Span = SourceSpanFactory.Span(48, 14) };

        var diagnostic = AssertReported(
            Check(GuardedByAKeyAnotherOptionQueries(query)), DiagnosticCatalog.KeyNeededBothWays);

        Assert.Equal(query.Span, diagnostic.Span);
        Assert.Equal(["Hero.Title", "a query", "menu", "a condition"], diagnostic.MessageArguments);
    }

    [Fact]
    public void Check_AnOptionsConditionKeyQueriedInItsJumpsText_Reports() =>
        AssertReported(Check(GuardedAndQueryingOneKeyInItsJumpsText()), DiagnosticCatalog.KeyNeededBothWays);

    [Fact]
    public void Check_AnOptionsConditionKeyQueriedAfterItsFirstLine_ReportsNothing() =>
        AssertNotReported(Check(GuardedAndQueryingOneKeyAfterItsFirstLine()));

    [Fact]
    public void Check_TwoOptionsGuardedByOneKey_ReportsNothing() =>
        AssertNotReported(Check(Choices(
            ConditionalChoice(Condition("Hero.Title"), Line(Text("Bow."))),
            ConditionalChoice(Condition("Hero.Title"), Line(Text("Kneel."))))));

    [Fact]
    public void Check_ARandomOptionsConditionKeyWeightingAnother_ReportsAtTheWeight()
    {
        var weight = QueryWeight("Guard.Mood") with { Span = SourceSpanFactory.Span(39, 13) };

        var diagnostic = AssertReported(
            Check(GuardedByAKeyAnotherRandomOptionIsWeightedBy(weight)), DiagnosticCatalog.KeyNeededBothWays);

        Assert.Equal(weight.Span, diagnostic.Span);
        Assert.Equal(["Guard.Mood", "a key weight", "random choice", "a condition"], diagnostic.MessageArguments);
    }

    [Fact]
    public void Check_ARandomOptionGuardedAndWeightedByOneKey_Reports() =>
        AssertReported(Check(GuardedAndWeightedByOneKey()), DiagnosticCatalog.KeyNeededBothWays);

    [Fact]
    public void Check_ARandomOptionsConditionKeyQueriedInItsLine_ReportsNothing() =>
        AssertNotReported(Check(RandomChoices(
            ConditionalRandomOption(
                Condition("Guard.Mood"), NumberWeight(50), Line(Text("You look "), Query("Guard.Mood"))),
            RandomOption(AutoWeight(), Line(Text("Move along."))))));

    [Fact]
    public void Check_AKeyQueriedTwiceAfterItsCondition_ReportsOnceAtTheFirstQuery()
    {
        var first = Query("Hero.Title") with { Span = SourceSpanFactory.Span(23, 14) };
        var second = Query("Hero.Title") with { Span = SourceSpanFactory.Span(42, 14) };

        var diagnostic = AssertReported(
            Check(ConditionalLine(Condition("Hero.Title"), Text("Hail, "), first, Text(", "), second)),
            DiagnosticCatalog.KeyNeededBothWays);

        Assert.Equal(first.Span, diagnostic.Span);
    }

    [Fact]
    public void Check_TwoKeysEachUsedBothWaysInOneMenu_ReportsEach()
    {
        var title = Query("Hero.Title") with { Span = SourceSpanFactory.Span(60, 14) };
        var rank = Query("Hero.Rank") with { Span = SourceSpanFactory.Span(80, 13) };

        var diagnostics = Check(TwoGuardsAnotherOptionQueries(title, rank));

        Assert.Equal([title.Span, rank.Span], diagnostics.Select(diagnostic => diagnostic.Span));
    }

    /// <summary>A line guarded by the key its <paramref name="query"/> reads.</summary>
    /// <remarks>
    /// <code>
    /// `Hero.Title?` Guard: Welcome, `"Hero.Title"`.
    /// </code>
    /// </remarks>
    private static Line GuardedAndQueryingOneKey(Query query) =>
        ConditionalLine(Condition(query.Key), Text("Welcome, "), query, Text("."));

    /// <summary>A line guarded by the key a query in its link reads.</summary>
    /// <remarks>
    /// <code>
    /// `Inn.Name?` Alice: Meet me at [the `"Inn.Name"` inn](#the-inn).
    /// </code>
    /// </remarks>
    private static Line GuardedAndQueryingOneKeyInALink() =>
        ConditionalLine(
            Condition("Inn.Name"),
            Text("Meet me at "),
            Link("#the-inn", Text("the "), Query("Inn.Name"), Text(" inn")),
            Text("."));

    /// <summary>
    /// A line guarded by the key a query in its closing jump's text reads. That text is never
    /// shown, so the query is never asked.
    /// </summary>
    /// <remarks>
    /// <code>
    /// `Inn.Name?` Alice: Follow me. =&gt; [to `"Inn.Name"`](#the-inn)
    /// </code>
    /// </remarks>
    private static Line GuardedAndQueryingOneKeyInItsJump() =>
        ConditionalLine(
            Condition("Inn.Name"), Text("Follow me. "), Jump("#the-inn", Text("to "), Query("Inn.Name")));

    /// <summary>
    /// A line querying the key its jump is guarded by. The jump's condition is asked when the line is
    /// left, apart from what the line says.
    /// </summary>
    /// <remarks>
    /// <code>
    /// Alice: You have `"Alice.HasKey"`. `Alice.HasKey?` =&gt; [Open the door](#the-vault)
    /// </code>
    /// </remarks>
    private static Line QueryingTheKeyItsJumpIsGuardedBy() =>
        Line(
            Text("You have "),
            Query("Alice.HasKey"),
            Text(". "),
            ConditionalJump(Condition("Alice.HasKey"), "#the-vault", Text("Open the door")));

    /// <summary>
    /// A menu whose first option is guarded by the key its second option's <paramref name="query"/>
    /// reads.
    /// </summary>
    /// <remarks>
    /// <code>
    /// - `Hero.Title?` Guard: Bow.
    /// - Guard: Who are you, `"Hero.Title"`?
    /// </code>
    /// </remarks>
    private static Choices GuardedByAKeyAnotherOptionQueries(Query query) =>
        Choices(
            ConditionalChoice(Condition(query.Key), Line(Text("Bow."))),
            Choice(Line(Text("Who are you, "), query, Text("?"))));

    /// <summary>A menu option guarded by the key a query in its jump's text reads.</summary>
    /// <remarks>
    /// <code>
    /// - `Ally.Name?` =&gt; [Ask `"Ally.Name"` to join](#join)
    /// - Alice: I'll go alone.
    /// </code>
    /// </remarks>
    private static Choices GuardedAndQueryingOneKeyInItsJumpsText() =>
        Choices(
            ConditionalChoice(
                Condition("Ally.Name"),
                ControlLine(Jump("#join", Text("Ask "), Query("Ally.Name"), Text(" to join")))),
            Choice(Line(Text("I'll go alone."))));

    /// <summary>
    /// A menu option guarded by a key that only a later line of its own reads. That line is said once
    /// the option is taken, apart from the menu.
    /// </summary>
    /// <remarks>
    /// <code>
    /// - `Ally.Name?` Alice: Come with me.
    ///
    ///   Alice: Welcome, `"Ally.Name"`.
    /// - Alice: I'll go alone.
    /// </code>
    /// </remarks>
    private static Choices GuardedAndQueryingOneKeyAfterItsFirstLine() =>
        Choices(
            ConditionalChoice(
                Condition("Ally.Name"),
                Line(Text("Come with me.")),
                Line(Text("Welcome, "), Query("Ally.Name"), Text("."))),
            Choice(Line(Text("I'll go alone."))));

    /// <summary>
    /// A random choice whose first random option is guarded by the key its second is weighted by.
    /// </summary>
    /// <remarks>
    /// <code>
    /// - `Guard.Mood?` `50%` Guard: Halt!
    /// - `Guard.Mood%` Guard: Lovely day.
    /// </code>
    /// </remarks>
    private static RandomChoices GuardedByAKeyAnotherRandomOptionIsWeightedBy(QueryWeight weight) =>
        RandomChoices(
            ConditionalRandomOption(Condition(weight.Key), NumberWeight(50), Line(Text("Halt!"))),
            RandomOption(weight, Line(Text("Lovely day."))));

    /// <summary>A random option guarded by the key it is weighted by.</summary>
    /// <remarks>
    /// <code>
    /// - `Guard.Mood?` `Guard.Mood%` Guard: Lovely day.
    /// - `%` Guard: Move along.
    /// </code>
    /// </remarks>
    private static RandomChoices GuardedAndWeightedByOneKey() =>
        RandomChoices(
            ConditionalRandomOption(Condition("Guard.Mood"), QueryWeight("Guard.Mood"), Line(Text("Lovely day."))),
            RandomOption(AutoWeight(), Line(Text("Move along."))));

    /// <summary>
    /// A menu whose first two options are guarded by the keys its third option's queries read.
    /// </summary>
    /// <remarks>
    /// <code>
    /// - `Hero.Title?` Guard: Bow.
    /// - `Hero.Rank?` Guard: Salute.
    /// - Guard: Hail, `"Hero.Title"` of `"Hero.Rank"`.
    /// </code>
    /// </remarks>
    private static Choices TwoGuardsAnotherOptionQueries(Query title, Query rank) =>
        Choices(
            ConditionalChoice(Condition(title.Key), Line(Text("Bow."))),
            ConditionalChoice(Condition(rank.Key), Line(Text("Salute."))),
            Choice(Line(Text("Hail, "), title, Text(" of "), rank, Text("."))));

    private static IReadOnlyList<Diagnostic> Check(params ScriptBlock[] body)
    {
        var bag = new DiagnosticBag();
        new KeyNeededBothWaysRule().Check(DialogueTreeIndex.Build(DesugaredDocument(body)), bag);
        return bag.Diagnostics;
    }
}

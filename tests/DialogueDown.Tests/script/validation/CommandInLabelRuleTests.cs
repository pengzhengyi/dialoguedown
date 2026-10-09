using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Validation;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DiagnosticsAssert;
using static DialogueDown.Tests.Support.DialogueAstFactory;

namespace DialogueDown.Tests.Script.Validation;

public sealed class CommandInLabelRuleTests
{
    [Fact]
    public void Check_ACommandInALinkLabel_ReportsAnErrorAtTheCommand()
    {
        var command = CustomCommand("Wave") with { Span = SourceSpanFactory.Span(16, 8) };

        var diagnostic = AssertReported(Check(InALinkLabel(command)), DiagnosticCatalog.CommandInLabel);

        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal(command.Span, diagnostic.Span);
    }

    [Fact]
    public void Check_ACommandInsideEmphasisInALabel_Reports() =>
        AssertReported(
            Check(InsideEmphasisInALinkLabel(CustomCommand("Wave"))), DiagnosticCatalog.CommandInLabel);

    [Fact]
    public void Check_TwoCommandsInOneLabel_ReportsEach()
    {
        var first = CustomCommand("Wave") with { Span = SourceSpanFactory.Span(8, 8) };
        var second = DefaultCommand("bow") with { Span = SourceSpanFactory.Span(21, 9) };

        var diagnostics = Check(BothInALinkLabel(first, second));

        Assert.Equal([first.Span, second.Span], diagnostics.Select(diagnostic => diagnostic.Span));
    }

    [Fact]
    public void Check_ACommandInSpeech_ReportsNothing() =>
        AssertNotReported(Check(InSpeech(CustomCommand("Wave"))));

    [Fact]
    public void Check_ACommandBesideALink_ReportsNothing() =>
        AssertNotReported(Check(BesideALink(CustomCommand("Wave"))));

    [Fact]
    public void Check_AQueryInALabel_ReportsNothing() =>
        AssertNotReported(Check(InALinkLabel(Query("PlaceName"))));

    [Fact]
    public void Check_ACommandInALabel_IsShownInItsCanonicalForm()
    {
        var command = CustomCommand("GiveQuest", "EmberCrown", "3");

        var diagnostic = AssertReported(Check(InALinkLabel(command)), DiagnosticCatalog.CommandInLabel);

        Assert.Equal([command.Canonical()], diagnostic.MessageArguments);
    }

    /// <summary>A line whose link label holds <paramref name="call"/>.</summary>
    /// <remarks>
    /// <code>
    /// Alice: [Talk to `Wave()`](#inn)
    /// </code>
    /// </remarks>
    private static Line InALinkLabel(InlineFragment call) =>
        Line(Link("#inn", Text("Talk to "), call));

    /// <summary>A line whose link label holds <paramref name="command"/> inside emphasis.</summary>
    /// <remarks>
    /// <code>
    /// Alice: [**Talk `Wave()`**](#inn)
    /// </code>
    /// </remarks>
    private static Line InsideEmphasisInALinkLabel(GameCall command) =>
        Line(Link("#inn", StyledText(SpeechStyle.Bold, Text("Talk "), command)));

    /// <summary>A line whose link label holds two commands.</summary>
    /// <remarks>
    /// <code>
    /// Alice: [`Wave()` and `("bow")`](#inn)
    /// </code>
    /// </remarks>
    private static Line BothInALinkLabel(GameCall first, GameCall second) =>
        Line(Link("#inn", first, Text(" and "), second));

    /// <summary>A line that performs <paramref name="command"/> between its words.</summary>
    /// <remarks>
    /// <code>
    /// Alice: Hi. `Wave()`
    /// </code>
    /// </remarks>
    private static Line InSpeech(GameCall command) =>
        Line(Text("Hi. "), command);

    /// <summary>A line with <paramref name="command"/> written beside a link, outside its brackets.</summary>
    /// <remarks>
    /// <code>
    /// Alice: `Wave()` [Talk to Bob](#inn)
    /// </code>
    /// </remarks>
    private static Line BesideALink(GameCall command) =>
        Line(command, Text(" "), Link("#inn", Text("Talk to Bob")));

    private static IReadOnlyList<Diagnostic> Check(ScriptBlock root)
    {
        var bag = new DiagnosticBag();
        var document = new DesugaredScriptDocument(new ScriptDocument([root]));
        new CommandInLabelRule().Check(DialogueTreeIndex.Build(document), bag);
        return bag.Diagnostics;
    }
}

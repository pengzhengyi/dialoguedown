using DialogueDown.Common;
using DialogueDown.Diagnostics;
using DialogueDown.Markdown;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Transpiler.Builders;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DiagnosticsAssert;
using static DialogueDown.Tests.Support.DialogueAstAssert;
using static DialogueDown.Tests.Support.MarkdownAstAssert;

namespace DialogueDown.Tests.Script.Transpiler.Builders;

public sealed class ControlBlockBuilderTests
{
    private readonly ControlBlockBuilder _builder = TranspilerBuilderFactory.ControlBlockBuilder();
    private readonly IMarkdownParser _parser = MarkdownParserFactory.MarkdownParser();

    [Fact]
    public void TryBuild_NonMarkerHeadedQuote_ReturnsFalse()
    {
        var quote = ParseQuote("> An aside.");

        var built = _builder.TryBuild(quote, new DiagnosticBag(), out var control);

        Assert.False(built);
        Assert.Null(control);
    }

    [Fact]
    public void TryBuild_MarkerHeadedQuote_SplitsBranches()
    {
        var quote = ParseQuote(
            """
            > `if` `Rich?`
            >
            > Alice: Welcome upstairs.
            >
            > `elseif` `Known?`
            >
            > Alice: Welcome back.
            >
            > `else`
            >
            > Alice: The common room is downstairs.
            """);

        var control = Build(quote);

        Assert.Equal(quote.Span, control.Span);
        Assert.Collection(
            control.Branches,
            branch =>
            {
                AssertCondition(branch.Condition!, "Rich");
                AssertSpeechText(AssertLine(Assert.Single(branch.Body)), "Welcome upstairs.");
            },
            branch =>
            {
                AssertCondition(branch.Condition!, "Known");
                AssertSpeechText(AssertLine(Assert.Single(branch.Body)), "Welcome back.");
            },
            branch =>
            {
                Assert.Null(branch.Condition);
                AssertSpeechText(
                    AssertLine(Assert.Single(branch.Body)),
                    "The common room is downstairs.");
            });
    }

    [Fact]
    public void TryBuild_NestedMarkerHeadedQuote_BuildsNestedControlBlock()
    {
        var quote = ParseQuote(
            """
            > `if` `Trusted?`
            >
            > > `if` `HasKey?`
            > >
            > > Alice: Open the vault.
            > >
            > > `else`
            > >
            > > Alice: Find the key.
            >
            > `else`
            >
            > Alice: Please leave.
            """);

        var control = Build(quote);

        var trusted = control.Branches[0];
        AssertCondition(trusted.Condition!, "Trusted");
        var inner = AssertControlBlock(Assert.Single(trusted.Body));
        AssertCondition(inner.Branches[0].Condition!, "HasKey");
        Assert.Null(inner.Branches[1].Condition);

        var untrusted = control.Branches[1];
        Assert.Null(untrusted.Condition);
        AssertSpeechText(AssertLine(Assert.Single(untrusted.Body)), "Please leave.");
    }

    [Fact]
    public void TryBuild_CarriesQuoteAndBranchSpans()
    {
        var quote = ParseQuote(
            """
            > `if` `Rich?`
            >
            > Welcome.
            >
            > `else`
            """);

        var control = Build(quote);

        Assert.Equal(quote.Span, control.Span);
        Assert.Equal(
            SourceSpan.Covering(quote.Blocks[0].Span, quote.Blocks[1].Span),
            control.Branches[0].Span);
        Assert.Equal(quote.Blocks[2].Span, control.Branches[1].Span);
    }

    [Fact]
    public void TryBuild_SeveredElseIf_ReportsAndBuildsAStandaloneRecovery()
    {
        var quote = ParseQuote(
            """
            > `elseif` `Known?`
            >
            > Alice: Welcome back.
            """);

        var control = Build(quote, out var diagnostics);

        var diagnostic = AssertReported(diagnostics.Diagnostics, "DLG1108");
        Assert.Single(diagnostics.Diagnostics);
        Assert.Equal(Assert.IsType<Paragraph>(quote.Blocks[0]).Inlines[0].Span, diagnostic.Span);
        var branch = Assert.Single(control.Branches);
        AssertCondition(branch.Condition!, "Known");
        AssertSpeechText(AssertLine(Assert.Single(branch.Body)), "Welcome back.");
    }

    [Fact]
    public void TryBuild_BranchesAfterElse_ReportMalformedOrder()
    {
        var quote = ParseQuote(
            """
            > `if` `Rich?`
            >
            > Welcome.
            >
            > `else`
            >
            > Try downstairs.
            >
            > `elseif` `Known?`
            >
            > Welcome back.
            >
            > `else`
            >
            > No entry.
            """);

        _ = Build(quote, out var diagnostics);

        Assert.Equal(
            2,
            diagnostics.Diagnostics.Count(diagnostic => diagnostic.Descriptor.Code == "DLG1109"));
        Assert.Equal(2, diagnostics.Diagnostics.Count);
    }

    [Fact]
    public void TryBuild_SecondIf_ReportsMalformedOrder()
    {
        var quote = ParseQuote(
            """
            > `if` `Rich?`
            >
            > Welcome.
            >
            > `if` `Known?`
            >
            > Welcome back.
            """);

        _ = Build(quote, out var diagnostics);

        AssertReported(diagnostics.Diagnostics, "DLG1109");
        Assert.Single(diagnostics.Diagnostics);
    }

    [Fact]
    public void TryBuild_FusedMarker_ReportsAndKeepsTrailingSpeechInTheBranchBody()
    {
        var quote = ParseQuote(
            """
            > `if` `Rich?`
            > Alice: Welcome upstairs.
            """);

        var control = Build(quote, out var diagnostics);

        var diagnostic = AssertReported(diagnostics.Diagnostics, "DLG1110");
        Assert.Single(diagnostics.Diagnostics);
        Assert.Equal(Assert.IsType<Paragraph>(quote.Blocks[0]).Inlines[^1].Span, diagnostic.Span);
        var branch = Assert.Single(control.Branches);
        AssertSpeechText(AssertLine(Assert.Single(branch.Body)), "Welcome upstairs.");
    }

    [Fact]
    public void TryBuild_IfWithoutCondition_ReportsMissingCondition()
    {
        var quote = ParseQuote(
            """
            > `if`
            >
            > Welcome.
            """);

        var control = Build(quote, out var diagnostics);

        AssertReported(diagnostics.Diagnostics, "DLG1111");
        Assert.Single(diagnostics.Diagnostics);
        Assert.Null(Assert.Single(control.Branches).Condition);
    }

    [Fact]
    public void TryBuild_ElseWithCondition_ReportsAndRecoversAsAnElseBranch()
    {
        var quote = ParseQuote(
            """
            > `if` `Rich?`
            >
            > Welcome.
            >
            > `else` `Known?`
            >
            > Welcome back.
            """);

        var control = Build(quote, out var diagnostics);

        AssertReported(diagnostics.Diagnostics, "DLG1112");
        Assert.Single(diagnostics.Diagnostics);
        Assert.Null(control.Branches[1].Condition);
        AssertSpeechText(AssertLine(Assert.Single(control.Branches[1].Body)), "Welcome back.");
    }

    private ControlBlock Build(QuoteBlock quote)
    {
        var control = Build(quote, out var diagnostics);
        Assert.Empty(diagnostics.Diagnostics);
        return control;
    }

    private ControlBlock Build(QuoteBlock quote, out DiagnosticBag diagnostics)
    {
        diagnostics = new DiagnosticBag();
        Assert.True(_builder.TryBuild(quote, diagnostics, out var control));
        return control;
    }

    private QuoteBlock ParseQuote(string source) =>
        AssertSingleBlock<QuoteBlock>(_parser.Parse(source));
}

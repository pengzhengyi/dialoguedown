using DialogueDown.Common;
using DialogueDown.Diagnostics;
using DialogueDown.Markdown;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Transpiler.Builders;
using DialogueDown.Tests.Support;
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

    private ControlBlock Build(QuoteBlock quote)
    {
        Assert.True(_builder.TryBuild(quote, new DiagnosticBag(), out var control));
        return control;
    }

    private QuoteBlock ParseQuote(string source) =>
        AssertSingleBlock<QuoteBlock>(_parser.Parse(source));
}

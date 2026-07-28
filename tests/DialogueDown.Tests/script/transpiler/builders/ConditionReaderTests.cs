using DialogueDown.Common;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Transpiler.Builders;
using static DialogueDown.Tests.Support.DialogueAstAssert;
using static DialogueDown.Tests.Support.MarkdownAstAssert;
using static DialogueDown.Tests.Support.MarkdownAstFactory;
using MarkdownInline = DialogueDown.Markdown.MarkdownInline;

namespace DialogueDown.Tests.Script.Transpiler.Builders;

public sealed class ConditionReaderTests
{
    [Theory]
    [InlineData("\"Rainy\"?", "Rainy")]
    [InlineData("\"Rainy?\"?", "Rainy?")]
    [InlineData(" \"Alice.Ready\" ? ", "Alice.Ready")]
    public void Read_AQueryFollowedByQuestionMark_YieldsACondition(string content, string expectedKey)
    {
        var condition = Assert.IsType<Condition>(
            ConditionReader.Read(content, new SourceSpan(0, content.Length)));

        Assert.Equal(expectedKey, condition.Key);
    }

    [Theory]
    [InlineData("\"Rainy\"")]     // a plain query, no ?
    [InlineData("?")]             // no query before the ?
    [InlineData("\"a\" \"b\"?")]  // not a single query before the ?
    [InlineData("(\"do\")?")]     // a command, not a query
    [InlineData("just words")]
    public void Read_AnythingElse_YieldsNull(string content) =>
        Assert.Null(ConditionReader.Read(content, new SourceSpan(0, content.Length)));

    [Fact]
    public void TryPeel_LeadingConditionThenContent_PeelsItAndTrimsTheRemainder()
    {
        MarkdownInline[] inlines = [CodeSpan("\"Rainy\"?"), Text(" hello")];

        var peeled = ConditionReader.TryPeel(inlines, out var condition, out var remainder);

        Assert.True(peeled);
        AssertCondition(condition, "Rainy");
        AssertSingleText(remainder, "hello");
    }

    [Fact]
    public void TryPeel_OnlyACondition_PeelsItWithAnEmptyRemainder()
    {
        MarkdownInline[] inlines = [CodeSpan("\"Rainy\"?")];

        var peeled = ConditionReader.TryPeel(inlines, out var condition, out var remainder);

        Assert.True(peeled);
        AssertCondition(condition, "Rainy");
        Assert.Empty(remainder);
    }

    [Fact]
    public void TryPeel_NoLeadingCondition_ReturnsFalseAndTheOriginalSequence()
    {
        MarkdownInline[] inlines = [Text("Alice: hello")];

        var peeled = ConditionReader.TryPeel(inlines, out _, out var remainder);

        Assert.False(peeled);
        Assert.Same(inlines, remainder);
    }

    [Fact]
    public void TryPeel_LeadingNonConditionCodeSpan_ReturnsFalse()
    {
        // A plain query (no `?`) is not a condition, so nothing is peeled.
        MarkdownInline[] inlines = [CodeSpan("\"Rainy\""), Text(" hello")];

        Assert.False(ConditionReader.TryPeel(inlines, out _, out _));
    }
}

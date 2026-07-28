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
    [InlineData("\"Rainy\"?", "Rainy")]                 // quoted key
    [InlineData("\"Rainy?\"?", "Rainy?")]               // quoted key escapes an inner ?
    [InlineData(" \"Alice.Ready\" ? ", "Alice.Ready")]  // whitespace around the key and sign
    [InlineData("Rainy?", "Rainy")]                     // unquoted key
    [InlineData("Alice.HasMap?", "Alice.HasMap")]       // unquoted dotted key
    [InlineData("Is Alice happy?", "Is Alice happy")]   // unquoted key keeps its spaces
    [InlineData(" IsReady ? ", "IsReady")]              // unquoted, surrounding whitespace trimmed
    [InlineData("\"a\" \"b\"?", "\"a\" \"b\"")]         // not one clean quoted string -> raw key
    [InlineData("(\"do\")?", "(\"do\")")]               // command-shaped text is just a raw key
    public void Read_AKeyFollowedByQuestionMark_YieldsACondition(string content, string expectedKey)
    {
        var condition = Assert.IsType<Condition>(
            ConditionReader.Read(content, new SourceSpan(0, content.Length)));

        Assert.Equal(expectedKey, condition.Key);
    }

    [Theory]
    [InlineData("\"Rainy\"")]  // a query with no ? is a value read, not a condition
    [InlineData("?")]          // no key before the ?
    [InlineData("   ?")]       // only whitespace before the ? -> empty key
    [InlineData("just words")] // no ? at all
    public void Read_WithoutATrailingQuestionMarkOrKey_YieldsNull(string content) =>
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
    public void TryPeel_LeadingUnquotedCondition_PeelsItAndTrimsTheRemainder()
    {
        MarkdownInline[] inlines = [CodeSpan("Is Alice happy?"), Text(" hello")];

        var peeled = ConditionReader.TryPeel(inlines, out var condition, out var remainder);

        Assert.True(peeled);
        AssertCondition(condition, "Is Alice happy");
        AssertSingleText(remainder, "hello");
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

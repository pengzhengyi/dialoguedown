using DialogueDown.Script.Transpiler.Parsing;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Script.Transpiler.Parsing;

public sealed class ParserExtensionsTests
{
    [Fact]
    public void ConsumeAll_WhenTheWholeInputMatches_PassesTheMatchThrough()
    {
        var result = TestParsers.Identifier.ConsumeAll(ParseInputFactory.Input("abc"));

        Assert.True(result.Success);
        Assert.Equal("abc", result.MatchedValue);
    }

    [Fact]
    public void ConsumeAll_WhenTextIsLeftOver_Fails()
    {
        // The identifier matches "abc" but " def" is left unconsumed.
        var result = TestParsers.Identifier.ConsumeAll(ParseInputFactory.Input("abc def"));

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public void ConsumeAll_WhenTheParserFails_PassesTheFailureThrough()
    {
        var result = TestParsers.Identifier.ConsumeAll(ParseInputFactory.Input("123"));

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public void TryParseAll_WhenTheWholeStringMatches_YieldsTheValue()
    {
        var matched = TestParsers.Identifier.TryParseAll("abc", out var value);

        Assert.True(matched);
        Assert.Equal("abc", value);
    }

    [Fact]
    public void TryParseAll_WhenTextIsLeftOverOrTheParserFails_YieldsFalse()
    {
        Assert.False(TestParsers.Identifier.TryParseAll("abc def", out _));
        Assert.False(TestParsers.Identifier.TryParseAll("123", out _));
    }

    [Fact]
    public void Explain_WithAFailure_AppendsTheTechnicalReason()
    {
        var failure = TestParsers.Identifier.Consume(ParseInputFactory.Input("123"));

        var message = failure.Explain("that is not an identifier");

        Assert.Contains("that is not an identifier", message);
        Assert.Contains("↳", message);
    }

    [Fact]
    public void Explain_WithoutAFailure_ReturnsTheHeadlineAlone()
    {
        var success = TestParsers.Identifier.Consume(ParseInputFactory.Input("abc"));

        Assert.Equal("all good", success.Explain("all good"));
    }
}

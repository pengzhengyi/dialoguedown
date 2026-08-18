using DialogueDown.Script.Transpiler.Parsing;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Script.Transpiler.Parsing;

public sealed class ParserCombinatorsTests
{
    [Fact]
    public void Select_TransformsTheValue_KeepingTheConsumedRange()
    {
        var parser = TestParsers.Identifier.Select(name => name.ToUpperInvariant());

        var result = parser.Consume(ParseInputFactory.Input("abc def", 5));

        Assert.Equal("ABC", result.MatchedValue);
        Assert.Equal(5, result.MatchedRange.Start);
        Assert.Equal(3, result.MatchedRange.Length);
    }

    [Fact]
    public void Located_PairsTheValueWithItsConsumedRange()
    {
        var parser = TestParsers.Identifier.Located();

        var result = parser.Consume(ParseInputFactory.Input("abc def", 5));

        Assert.Equal("abc", result.MatchedValue.Value);
        Assert.Equal(5, result.MatchedValue.Range.Start);
        Assert.Equal(3, result.MatchedValue.Range.Length);
    }

    [Fact]
    public void Select_PropagatesFailure()
    {
        var parser = TestParsers.Identifier.Select(name => name.ToUpperInvariant());

        var result = parser.Consume(ParseInputFactory.Input("123"));

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public void SelectMany_SequencesParsers_ThreadingTheAbsolutePosition()
    {
        var parser =
            from first in TestParsers.Identifier
            from _ in TestParsers.Symbol('-')
            from second in TestParsers.Identifier
            select $"{first}+{second}";

        var result = parser.Consume(ParseInputFactory.Input("ab-cd", 10));

        Assert.Equal("ab+cd", result.MatchedValue);
        Assert.Equal(10, result.MatchedRange.Start);
        Assert.Equal(5, result.MatchedRange.Length);
    }

    [Fact]
    public void SelectMany_PropagatesFailureOfTheFirstParser()
    {
        var parser =
            from first in TestParsers.Identifier
            from second in TestParsers.Identifier
            select $"{first}{second}";

        var result = parser.Consume(ParseInputFactory.Input("-nope"));

        Assert.False(result.Success);
    }

    [Fact]
    public void SelectMany_PropagatesFailureOfTheSecondParser()
    {
        var parser =
            from first in TestParsers.Identifier
            from _ in TestParsers.Symbol('-')
            from second in TestParsers.Identifier
            select $"{first}+{second}";

        // "ab-" has no second identifier, so the second parser fails.
        var result = parser.Consume(ParseInputFactory.Input("ab-"));

        Assert.False(result.Success);
    }

    [Fact]
    public void Optional_Present_ReturnsTheValueAndItsRange()
    {
        var parser = TestParsers.Identifier.Optional();

        var result = parser.Consume(ParseInputFactory.Input("abc", 5));

        Assert.Equal("abc", result.MatchedValue);
        Assert.Equal(3, result.MatchedLength);
    }

    [Fact]
    public void Optional_Absent_SucceedsWithDefault_ConsumingNothing()
    {
        var parser = TestParsers.Identifier.Optional();

        var result = parser.Consume(ParseInputFactory.Input("123", 5));

        Assert.True(result.Success);
        Assert.Null(result.MatchedValue);
        Assert.Equal(0, result.MatchedLength);
        Assert.Equal(5, result.MatchedRange.Start);
    }

    [Fact]
    public void Optional_InSequence_WhenAbsent_LeavesThePositionForTheNextParser()
    {
        var parser =
            from name in TestParsers.Identifier.Optional()
            from mark in TestParsers.Symbol('!')
            select $"{name ?? "<none>"}{mark}";

        var absent = parser.Consume(ParseInputFactory.Input("!"));
        Assert.Equal("<none>!", absent.MatchedValue);
        Assert.Equal(1, absent.MatchedLength);

        var present = parser.Consume(ParseInputFactory.Input("ab!"));
        Assert.Equal("ab!", present.MatchedValue);
        Assert.Equal(3, present.MatchedLength);
    }

    [Fact]
    public void Repeated_CollectsConsecutiveMatches_ThreadingThePosition()
    {
        var parser = TestParsers.Symbol('a').Repeated();

        var result = parser.Consume(ParseInputFactory.Input("aaab", 10));

        Assert.Equal(new[] { 'a', 'a', 'a' }, result.MatchedValue);
        Assert.Equal(10, result.MatchedRange.Start);
        Assert.Equal(3, result.MatchedLength);
    }

    [Fact]
    public void Repeated_NoMatch_SucceedsWithAnEmptyList_ConsumingNothing()
    {
        var parser = TestParsers.Symbol('a').Repeated();

        var result = parser.Consume(ParseInputFactory.Input("bbb", 5));

        Assert.True(result.Success);
        Assert.Empty(result.MatchedValue);
        Assert.Equal(0, result.MatchedLength);
        Assert.Equal(5, result.MatchedRange.Start);
    }

    [Fact]
    public void Repeated_StopsOnANonConsumingMatch_RatherThanLooping()
    {
        // The inner parser always succeeds (Optional), matching nothing once the
        // 'a's run out; the empty-match condition stops the loop instead of spinning.
        var parser = TestParsers.Symbol('a').Optional().Repeated();

        var result = parser.Consume(ParseInputFactory.Input("aab"));

        Assert.Equal(2, result.MatchedValue.Count);
        Assert.Equal(2, result.MatchedLength);
    }

    [Fact]
    public void Or_UsesTheFirstParserWhenItMatches()
    {
        var parser = TestParsers.Symbol('a').Or(TestParsers.Symbol('b'));

        var result = parser.Consume(ParseInputFactory.Input("a"));

        Assert.True(result.Success);
        Assert.Equal('a', result.MatchedValue);
    }

    [Fact]
    public void Or_FallsBackToTheSecondParser()
    {
        var parser = TestParsers.Symbol('a').Or(TestParsers.Symbol('b'));

        var result = parser.Consume(ParseInputFactory.Input("b"));

        Assert.True(result.Success);
        Assert.Equal('b', result.MatchedValue);
    }

    [Fact]
    public void Or_FailsWhenNeitherMatches()
    {
        var parser = TestParsers.Symbol('a').Or(TestParsers.Symbol('b'));
        Assert.False(parser.Consume(ParseInputFactory.Input("c")).Success);
    }

    [Fact]
    public void OptionalOrDefault_Present_ReturnsTheValue() =>
        Assert.Equal("abc", TestParsers.Identifier.OptionalOrDefault("<none>")
            .Consume(ParseInputFactory.Input("abc")).MatchedValue);

    [Fact]
    public void OptionalOrDefault_Absent_ReturnsTheDefault_ConsumingNothing()
    {
        var result = TestParsers.Identifier.OptionalOrDefault("<none>")
            .Consume(ParseInputFactory.Input("123"));

        Assert.Equal("<none>", result.MatchedValue);
        Assert.Equal(0, result.MatchedLength);
    }
}

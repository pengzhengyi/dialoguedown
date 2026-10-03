using DialogueDown.Script.Transpiler.Parsing;

namespace DialogueDown.Tests.Script.Transpiler.Parsing;

public sealed class TextRangeTests
{
    [Fact]
    public void ExposesStartLengthAndComputedEnd()
    {
        var range = new TextRange(5, 3);

        Assert.Equal(5, range.Start);
        Assert.Equal(3, range.Length);
        Assert.Equal(8, range.End);
    }

    [Fact]
    public void AllowsEmptyRange()
    {
        var range = new TextRange(5, 0);

        Assert.Equal(0, range.Length);
        Assert.Equal(5, range.End);
    }

    [Fact]
    public void RejectsNegativeStart() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextRange(-1, 0));

    [Fact]
    public void RejectsNegativeLength() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextRange(0, -1));

    [Fact]
    public void ToSourceSpan_PreservesStartAndLength()
    {
        var span = new TextRange(5, 3).ToSourceSpan();

        Assert.Equal(5, span.Start);
        Assert.Equal(3, span.Length);
    }

    [Fact]
    public void ToSourceSpan_EmptyRange_Throws() =>
        // A parsed node always covers at least one character, so an empty range cannot
        // become a SourceSpan (a synthetic node uses SourceSpan.EmptyAt instead).
        Assert.Throws<InvalidOperationException>(() => new TextRange(5, 0).ToSourceSpan());

    [Fact]
    public void Plus_JoinsContiguousRanges_FromFirstStartToSecondEnd()
    {
        var joined = new TextRange(2, 3) + new TextRange(5, 4); // [2,5) + [5,9)

        Assert.Equal(2, joined.Start);
        Assert.Equal(9, joined.End);
        Assert.Equal(7, joined.Length);
    }

    [Fact]
    public void Plus_AnEmptyRangeAtTheBoundary_IsAllowed()
    {
        var joined = new TextRange(2, 3) + new TextRange(5, 0); // [2,5) + [5,5)

        Assert.Equal(2, joined.Start);
        Assert.Equal(5, joined.End);
    }

    [Fact]
    public void Plus_RangesWithAGapBetween_Throws() =>
        // [2,5) then [6,8) leaves [5,6) uncovered, so joining would also cover the gap.
        Assert.Throws<ArgumentException>(() => new TextRange(2, 3) + new TextRange(6, 2));

    [Fact]
    public void Plus_OverlappingRanges_Throws() =>
        Assert.Throws<ArgumentException>(() => new TextRange(2, 5) + new TextRange(3, 4));
}

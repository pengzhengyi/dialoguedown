using DialogueDown.Diagnostics;

namespace DialogueDown.Tests.Diagnostics;

public sealed class LineMapTests
{
    [Theory]
    // Single line: first, middle, last character, and the end-of-source position.
    [InlineData("hello", 0, 1, 1)]
    [InlineData("hello", 4, 1, 5)]
    [InlineData("hello", 5, 1, 6)]
    // Multi-line (LF): the '\n' sits on its line; the next line starts after it.
    [InlineData("ab\ncd", 0, 1, 1)]
    [InlineData("ab\ncd", 2, 1, 3)] // the '\n'
    [InlineData("ab\ncd", 3, 2, 1)] // 'c'
    [InlineData("ab\ncd", 5, 2, 3)] // end of source
    // CRLF: the '\r' is the line's last column, the '\n' ends the line.
    [InlineData("a\r\nb", 1, 1, 2)] // the '\r'
    [InlineData("a\r\nb", 2, 1, 3)] // the '\n'
    [InlineData("a\r\nb", 3, 2, 1)] // 'b'
    [InlineData("a\r\nb", 4, 2, 2)] // end of source
    // Empty source: one line, column one.
    [InlineData("", 0, 1, 1)]
    // The end of the source, with and without a trailing newline.
    [InlineData("abc", 3, 1, 4)]
    [InlineData("abc\n", 4, 2, 1)]
    public void Locate_MapsOffsetToOneBasedLineAndColumn(string source, int offset, int line, int column) =>
        AssertLocation(new LineMap(source), offset, line, column);

    [Fact]
    public void Locate_AfterABlankLine_CountsTheEmptyLine()
    {
        // "a\n\nb": line 1 "a", line 2 "" (just the second '\n'), line 3 "b".
        var map = new LineMap("a\n\nb");

        AssertLocation(map, 2, 2, 1); // line 2 starts at the second '\n'
        AssertLocation(map, 3, 3, 1); // 'b'
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    public void Locate_AnOffsetOutsideTheSource_Throws(int offset)
    {
        var map = new LineMap("hello"); // length 5, so valid offsets are 0..5

        Assert.Throws<ArgumentOutOfRangeException>(() => map.Locate(offset));
    }

    [Fact]
    public void Constructor_NullSource_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new LineMap(null!));

    private static void AssertLocation(LineMap map, int offset, int line, int column) =>
        Assert.Equal(new LinePosition(line, column), map.Locate(offset));
}

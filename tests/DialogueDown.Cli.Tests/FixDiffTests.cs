using DialogueDown.Cli.Fixing;
using static DialogueDown.Cli.Tests.Support.LocatedDiagnosticFactory;

namespace DialogueDown.Cli.Tests;

public sealed class FixDiffTests
{
    [Fact]
    public void Rows_InsertionInTheMiddle_ShowsContextMinusAndPlus()
    {
        const string Source = """
            # The Workshop

            Alice: The rule is simple => the lever opens the door.

            *Bob*: Did you read the manual?

            """;
        var arrow = Source.IndexOf("=>", StringComparison.Ordinal);
        var fix = Escape(arrow);

        var rows = FixDiff.Rows(Source, fix);

        Assert.Equal([' ', '-', '+', ' '], rows.Select(row => row.Marker));
        Assert.Equal("Alice: The rule is simple => the lever opens the door.", rows[1].Text);
        Assert.Equal("Alice: The rule is simple \\=> the lever opens the door.", rows[2].Text);
        // Every row carries the line number of its own side.
        Assert.Equal([2, 3, 3, 4], rows.Select(row => row.LineNumber));
    }

    [Fact]
    public void Rows_Insertion_EmphasizesTheChangedWords()
    {
        const string Source = "Alice: The rule is simple => the lever opens the door.\n";
        var arrow = Source.IndexOf("=>", StringComparison.Ordinal);
        var fix = Escape(arrow);

        var rows = FixDiff.Rows(Source, fix);
        var removed = Assert.Single(rows, row => row.Marker == '-');
        var added = Assert.Single(rows, row => row.Marker == '+');

        // An insertion emphasizes the replaced word on each side; the prose stays whole.
        Assert.Equal(["=>"], removed.Segments.Where(segment => segment.Changed).Select(segment => segment.Text));
        Assert.Equal(["\\=>"], added.Segments.Where(segment => segment.Changed).Select(segment => segment.Text));
        Assert.Equal(
            "Alice: The rule is simple ",
            string.Concat(removed.Segments.TakeWhile(segment => !segment.Changed).Select(segment => segment.Text)));
    }

    [Fact]
    public void Rows_FirstLine_ShowsNoContextBefore()
    {
        var fix = Fix("Append", Insert(1, "b"));

        Assert.Equal(["-a", "+ab"], FixDiff.Rows("a\n", fix).Select(row => row.Marker + row.Text));
    }

    [Fact]
    public void Rows_LastLineWithoutATrailingNewline_ShowsTheContextLine()
    {
        var fix = Fix("Append", Insert(3, "b"));

        Assert.Equal([" x", "-a", "+ab"], FixDiff.Rows("x\na", fix).Select(row => row.Marker + row.Text));
    }

    [Fact]
    public void Rows_CrLfText_TrimsTheCarriageReturn()
    {
        var fix = Escape(7);

        Assert.Equal(
            ["-Alice: => x", "+Alice: \\=> x"],
            FixDiff.Rows("Alice: => x\r\n", fix).Select(row => row.Marker + row.Text));
    }

    [Fact]
    public void Rows_ATrailingNewline_IsNotContext()
    {
        var fix = Escape(7);

        Assert.Equal(
            ["-Alice: => x", "+Alice: \\=> x"],
            FixDiff.Rows("Alice: => x\n", fix).Select(row => row.Marker + row.Text));
    }

    [Fact]
    public void Rows_NoLineChange_RendersNothing()
    {
        var fix = Fix("No-op", Insert(0, string.Empty));

        Assert.Empty(FixDiff.Rows("a\n", fix));
    }
}

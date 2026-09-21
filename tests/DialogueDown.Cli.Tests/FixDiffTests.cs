using DialogueDown.Cli.Fixing;
using DialogueDown.Diagnostics;

namespace DialogueDown.Cli.Tests;

public sealed class FixDiffTests
{
    [Fact]
    public void Rows_InsertionInTheMiddle_ShowsContextMinusAndPlus()
    {
        const string Source =
            "# The Workshop\n\nAlice: The rule is simple => the lever opens the door.\n\n*Bob*: Did you read the manual?\n";
        var arrow = Source.IndexOf("=>", StringComparison.Ordinal);
        var fix = new LocatedFix("Escape as literal text", [new LocatedEdit(arrow, arrow, "\\")]);

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
        var fix = new LocatedFix("Escape as literal text", [new LocatedEdit(arrow, arrow, "\\")]);

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
        var fix = new LocatedFix("Append", [new LocatedEdit(1, 1, "b")]);

        Assert.Equal(["-a", "+ab"], FixDiff.Rows("a\n", fix).Select(row => row.Marker + row.Text));
    }

    [Fact]
    public void Rows_LastLineWithoutATrailingNewline_ShowsTheContextLine()
    {
        var fix = new LocatedFix("Append", [new LocatedEdit(3, 3, "b")]);

        Assert.Equal([" x", "-a", "+ab"], FixDiff.Rows("x\na", fix).Select(row => row.Marker + row.Text));
    }

    [Fact]
    public void Rows_CrLfText_TrimsTheCarriageReturn()
    {
        var fix = new LocatedFix("Escape as literal text", [new LocatedEdit(7, 7, "\\")]);

        Assert.Equal(
            ["-Alice: => x", "+Alice: \\=> x"],
            FixDiff.Rows("Alice: => x\r\n", fix).Select(row => row.Marker + row.Text));
    }

    [Fact]
    public void Rows_ATrailingNewline_IsNotContext()
    {
        var fix = new LocatedFix("Escape as literal text", [new LocatedEdit(7, 7, "\\")]);

        Assert.Equal(
            ["-Alice: => x", "+Alice: \\=> x"],
            FixDiff.Rows("Alice: => x\n", fix).Select(row => row.Marker + row.Text));
    }

    [Fact]
    public void Rows_NoLineChange_RendersNothing()
    {
        var fix = new LocatedFix("No-op", [new LocatedEdit(0, 0, string.Empty)]);

        Assert.Empty(FixDiff.Rows("a\n", fix));
    }
}

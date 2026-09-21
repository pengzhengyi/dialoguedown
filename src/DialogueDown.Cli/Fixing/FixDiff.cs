using DialogueDown.Diagnostics;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Spectre.Console;

namespace DialogueDown.Cli.Fixing;

/// <summary>
/// Renders one applied fix as a compact, git-style hunk: the changed line as a removal and an
/// addition, with a line of context either side, and the changed words emphasized on a terminal.
/// The hunk is computed with DiffPlex over the script with and without that one fix, so it shows
/// the change the writer's file received rather than the edit the compiler holds.
/// </summary>
internal static class FixDiff
{
    private const int ContextLines = 1;

    /// <summary>The hunk rows for one applied fix, or empty when the fix changes no line.</summary>
    public static IReadOnlyList<HunkRow> Rows(string source, LocatedFix fix)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(fix);

        var after = FixApplier.Splice(source, [fix]);
        var model = new SideBySideDiffBuilder(new Differ()).BuildDiffModel(source, after);
        var rows = model.OldText.Lines.Count;
        if (rows > 0
            && source.EndsWith('\n')
            && model.OldText.Lines[rows - 1].Text.Length == 0
            && model.NewText.Lines[rows - 1].Text.Length == 0)
        {
            rows--; // the virtual line after a trailing newline is not context
        }

        var changed = Enumerable.Range(0, rows)
            .Where(row => model.OldText.Lines[row].Type != ChangeType.Unchanged
                || model.NewText.Lines[row].Type != ChangeType.Unchanged)
            .ToList();
        if (changed.Count == 0)
        {
            return [];
        }

        var hunk = new List<HunkRow>();
        var first = Math.Max(0, changed[0] - ContextLines);
        var last = Math.Min(rows - 1, changed[^1] + ContextLines);
        for (var row = first; row <= last; row++)
        {
            var oldLine = model.OldText.Lines[row];
            var newLine = model.NewText.Lines[row];
            if (oldLine.Type == ChangeType.Unchanged && newLine.Type == ChangeType.Unchanged)
            {
                hunk.Add(new HunkRow(' ', [new HunkSegment(newLine.Text, Changed: false)]));
                continue;
            }

            if (oldLine.Type is not (ChangeType.Inserted or ChangeType.Imaginary))
            {
                hunk.Add(new HunkRow('-', Segments(oldLine, removed: true)));
            }

            if (newLine.Type is not (ChangeType.Deleted or ChangeType.Imaginary))
            {
                hunk.Add(new HunkRow('+', Segments(newLine, removed: false)));
            }
        }

        return hunk;
    }

    /// <summary>Writes the hunk: colored and with the changed words emphasized on a terminal, plain otherwise.</summary>
    public static void Render(IAnsiConsole console, IReadOnlyList<HunkRow> rows)
    {
        ArgumentNullException.ThrowIfNull(console);
        ArgumentNullException.ThrowIfNull(rows);

        foreach (var row in rows)
        {
            if (!console.Profile.Capabilities.Interactive)
            {
                console.WriteLine(row.Marker + row.Text);
                continue;
            }

            var color = ColorOf(row.Marker);
            var body = string.Concat(row.Segments.Select(segment =>
                segment.Changed
                    ? $"[bold {color}]{Markup.Escape(segment.Text)}[/]"
                    : $"[{color}]{Markup.Escape(segment.Text)}[/]"));
            console.MarkupLine($"[{color}]{row.Marker}[/]{body}");
        }
    }

    // A changed line's words come from DiffPlex's sub-pieces: the side we keep is unchanged prose,
    // and the side we drop is the emphasis. A line with no sub-pieces changed whole.
    private static IReadOnlyList<HunkSegment> Segments(DiffPiece piece, bool removed)
    {
        if (piece.SubPieces.Count == 0)
        {
            return [new HunkSegment(piece.Text, Changed: false)];
        }

        var dropped = removed ? ChangeType.Inserted : ChangeType.Deleted;
        return
        [
            .. piece.SubPieces
                .Where(sub => sub.Type != dropped)
                .Select(sub => new HunkSegment(sub.Text, Changed: sub.Type != ChangeType.Unchanged)),
        ];
    }

    private static string ColorOf(char marker) => marker switch
    {
        '-' => "red",
        '+' => "green",
        _ => "grey",
    };
}

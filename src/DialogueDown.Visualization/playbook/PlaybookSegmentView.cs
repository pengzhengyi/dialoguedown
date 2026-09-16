namespace DialogueDown.Visualization.Playbook;

/// <summary>One labeled piece of a node's summary.</summary>
/// <param name="Text">The piece's text. Joining every piece's text rebuilds the summary.</param>
/// <param name="Role">What the piece is, as <see cref="SummaryRoles"/> names it.</param>
/// <param name="Target">
/// The node this piece names, for a piece that says where control can go. A piece that carries one
/// can be followed, so the reader reaches the node rather than a number that stands for it.
/// </param>
internal sealed record PlaybookSegmentView(string Text, string Role, int? Target = null)
{
    /// <summary>A speaker's name, or the stand-in standing where a name would be.</summary>
    /// <param name="text">The name to write.</param>
    /// <returns>The piece.</returns>
    public static PlaybookSegmentView Speaker(string text) => new(text, SummaryRoles.Speaker);

    /// <summary>The words a writer wrote.</summary>
    /// <param name="text">The words to write.</param>
    /// <returns>The piece.</returns>
    public static PlaybookSegmentView Plain(string text) => new(text, SummaryRoles.Plain);

    /// <summary>The table's own words.</summary>
    /// <param name="text">The words to write.</param>
    /// <returns>The piece.</returns>
    public static PlaybookSegmentView Keyword(string text) => new(text, SummaryRoles.Keyword);

    /// <summary>The table's own punctuation.</summary>
    /// <param name="text">The punctuation to write.</param>
    /// <returns>The piece.</returns>
    public static PlaybookSegmentView Separator(string text) => new(text, SummaryRoles.Separator);

    /// <summary>The punctuation dividing one item of the summary from the next.</summary>
    /// <param name="text">The punctuation to write.</param>
    /// <returns>The piece.</returns>
    public static PlaybookSegmentView Boundary(string text) => new(text, SummaryRoles.Boundary);

    /// <summary>Where a list begins: the boundary before its first item, which carries nothing.</summary>
    /// <returns>The piece.</returns>
    public static PlaybookSegmentView Opening() => new(string.Empty, SummaryRoles.Boundary);

    /// <summary>A node the summary names, which is also a way to reach it.</summary>
    /// <param name="text">How the summary writes the node — its number, or a jump's own words.</param>
    /// <param name="target">The node the piece names.</param>
    /// <returns>The piece.</returns>
    public static PlaybookSegmentView LinkedTo(string text, int target) =>
        new(text, SummaryRoles.Target, target);

    /// <summary>A command the host performs.</summary>
    /// <param name="text">The command to write.</param>
    /// <returns>The piece.</returns>
    public static PlaybookSegmentView Command(string text) => new(text, SummaryRoles.Command);

    /// <summary>A placeholder only a running game can answer.</summary>
    /// <param name="text">The placeholder to write.</param>
    /// <returns>The piece.</returns>
    public static PlaybookSegmentView Query(string text) => new(text, SummaryRoles.Query);

    /// <summary>A marker standing where a value is missing.</summary>
    /// <param name="text">The marker to write.</param>
    /// <returns>The piece.</returns>
    public static PlaybookSegmentView Absent(string text) => new(text, SummaryRoles.Absent);
}

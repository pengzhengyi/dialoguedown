namespace DialogueDown.Visualization.Playbook;

/// <summary>One labeled piece of a node's summary.</summary>
/// <param name="Text">The piece's text. Joining every piece's text rebuilds the summary.</param>
/// <param name="Role">What the piece is, as <see cref="SummaryRoles"/> names it.</param>
internal sealed record PlaybookSegmentView(string Text, string Role)
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

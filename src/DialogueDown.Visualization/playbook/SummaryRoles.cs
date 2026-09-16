namespace DialogueDown.Visualization.Playbook;

/// <summary>
/// What a summary segment is, as the report's payload names it.
/// </summary>
/// <remarks>
/// A role is a fact about the text rather than a way of drawing it, so these names are a wire
/// contract: the client's <c>SummaryRole</c> union lists exactly these, and each one maps to a class
/// of its own. They are plain strings rather than an enum because a string is what crosses the wire,
/// the same way <c>NodeKinds</c> names a node's kind.
/// </remarks>
internal static class SummaryRoles
{
    /// <summary>Who a line belongs to, including the stand-ins for a name it lacks.</summary>
    public const string Speaker = "speaker";

    /// <summary>The words a writer wrote.</summary>
    public const string Plain = "plain";

    /// <summary>The table's own words: IF, THEN, ELSE, END, CONTINUE, DRAW 1 FROM.</summary>
    public const string Keyword = "keyword";

    /// <summary>The table's own punctuation, and the ellipsis a cut summary trails.</summary>
    public const string Separator = "separator";

    /// <summary>A command the host performs, written in round brackets.</summary>
    public const string Command = "command";

    /// <summary>A placeholder only a running game can answer.</summary>
    public const string Query = "query";

    /// <summary>A marker standing where a value is missing: &lt;no speech&gt;, &lt;no label&gt;.</summary>
    public const string Absent = "absent";
}

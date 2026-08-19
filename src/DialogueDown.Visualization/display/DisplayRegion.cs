namespace DialogueDown.Visualization.Display;

/// <summary>
/// A named area of the document a stage's nodes sit in — a scene today, a file later.
/// </summary>
/// <remarks>
/// A region is metadata rather than flow, so it is carried beside the nodes rather than as edges
/// that would imply control enters a grouping. It names itself (<see cref="Name"/>), says what
/// kind of grouping it is (<see cref="Kind"/>), and points back at the text that declares it
/// (<see cref="Span"/>) — a scene's heading — so a reader can be taken to where a region begins
/// rather than only to the lines inside it. <see cref="Anchor"/> is the slug a divert names it by.
/// </remarks>
public sealed record DisplayRegion(string Name, string Kind, string? Anchor = null)
{
    /// <summary>
    /// Where the region is declared, as a half-open character range into the original document —
    /// a scene's heading text. Null when the declaring text cannot be located.
    /// </summary>
    public DisplaySpan? Span { get; init; }
}

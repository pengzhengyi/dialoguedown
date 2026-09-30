namespace DialogueDown.Runtime.Situations;

/// <summary>
/// Where in a node a run is waiting on the world.
/// </summary>
/// <remarks>
/// A run asks the world about a node before it plays, again before a line continues after a
/// command, and again before it leaves, because the node may change the world in between. The
/// moment travels with the wait and says which of these the answers are for.
/// </remarks>
public abstract record Moment
{
    // Private, so the two kinds nested here are the only ones there can be.
    private Moment()
    {
    }

    /// <summary>Gets the moment before a node plays from its start.</summary>
    public static Moment BeforePlaying { get; } = new ToPlay(0);

    /// <summary>Gets the moment before a run leaves a node.</summary>
    public static Moment BeforeLeaving { get; } = new ToLeave();

    /// <summary>The moment before a line continues from one of its segments.</summary>
    /// <param name="segmentIndex">The index of the segment the line continues from.</param>
    /// <returns>The moment.</returns>
    public static Moment BeforeContinuingFrom(int segmentIndex) => new ToPlay(segmentIndex);

    /// <summary>Before playing: whether the node plays, and what its words say.</summary>
    /// <param name="SegmentIndex">
    /// The index of the segment playing starts from, among the line's segments; zero when the node
    /// plays from its start.
    /// </param>
    public sealed record ToPlay(int SegmentIndex) : Moment;

    /// <summary>Before leaving: which of the node's ways out the run takes.</summary>
    public sealed record ToLeave : Moment;
}

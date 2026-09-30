namespace DialogueDown.Runtime.Situations;

/// <summary>
/// Where a node carries on once the host has done what it asked.
/// </summary>
/// <remarks>
/// A line can stop part-way through to wait on the host, and the runner remembers nothing between
/// steps, so the place it carries on from travels with the wait.
/// </remarks>
public abstract record Resume
{
    // Private, so the two kinds nested here are the only ones there can be.
    private Resume()
    {
    }

    /// <summary>The line continues from one of its segments.</summary>
    /// <param name="SegmentIndex">The segment's index among the line's segments, counting from zero.</param>
    public sealed record From(int SegmentIndex) : Resume;

    /// <summary>The node has finished playing, so its kind decides what follows.</summary>
    public sealed record FromNodeEnd : Resume;
}

using DialogueDown.Playbook.Edges;

namespace DialogueDown.Playbook.Weights;

/// <summary>
/// Decides which of a random choice's random options are excluded from the draw.
/// </summary>
/// <remarks>
/// An excluded random option has a share of zero, and the random options in play share the draw
/// as if it were not written. The runner, for example, excludes a random option whose condition
/// the world withholds.
/// </remarks>
public interface IRandomOptionArbiter
{
    /// <summary>Whether a random option is excluded from the draw.</summary>
    /// <param name="option">One of the random choice's random options.</param>
    /// <returns>
    /// <see langword="true"/> when the random option is excluded; <see langword="false"/> when it
    /// is in play.
    /// </returns>
    bool Excludes(RandomOptionEdge option);
}

using DialogueDown.Runtime.Positions;

namespace DialogueDown.Runtime;

/// <summary>
/// Everything about a run that changes as it goes.
/// </summary>
/// <remarks>
/// Small on purpose, because the host owns the game. A host that wants "only once" answers a
/// query it owns, so visit counts belong to the world.
/// </remarks>
/// <param name="Position">Where the run stands, and at what stage.</param>
public sealed record PlayState(Position Position)
{
    /// <summary>Gets the state a run has before it begins.</summary>
    public static PlayState Initial { get; } = new(new NotStarted());
}

using DialogueDown.Runtime.Situations;

namespace DialogueDown.Runtime;

/// <summary>
/// Everything about a run that changes as it goes.
/// </summary>
/// <remarks>
/// Holds only the situation. Game state, such as how often a line has played, lives in the host,
/// which answers queries about it.
/// </remarks>
/// <param name="Situation">Which node the run has reached, and what it is doing there.</param>
public sealed record PlayState(Situation Situation)
{
    /// <summary>Gets the state a run has before it begins.</summary>
    public static PlayState Initial { get; } = new(new NotStarted());
}

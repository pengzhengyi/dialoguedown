namespace DialogueDown.Playbook.Weights;

/// <summary>
/// Gives each key weight the number the world answered for it.
/// </summary>
/// <remarks>
/// A key weight, such as <c>`Guard.Mood%`</c>, has no number in the playbook, because how likely
/// its random option is belongs to the running game. Answered <c>20</c>, it counts as
/// <c>`20%`</c> would. The runner, for example, gives the numbers the world supplied.
/// </remarks>
public interface IKeyWeightAnswers
{
    /// <summary>The number the world answered for a key weight.</summary>
    /// <param name="key">The key the key weight asks about, such as <c>Guard.Mood</c>.</param>
    /// <returns>The number, which counts as a percentage written in the key weight's place.</returns>
    double NumberFor(string key);
}

namespace DialogueDown.Runtime.Protocol;

/// <summary>
/// The world answers with a number.
/// </summary>
/// <remarks>
/// The answer a key weight needs. A random option can have a key weight —
/// <c>- `Guard.Mood%` Guard: Lovely day.</c> — because how likely it is belongs to the running
/// game: the guard's mood rises and falls with what the player has done. The number stands for
/// the random option's weight, so an answer of <c>20</c> counts as though <c>`20%`</c> were
/// written in its place.
/// </remarks>
/// <param name="Value">The number the world gave.</param>
public sealed record NumberAnswer(double Value) : Answer;

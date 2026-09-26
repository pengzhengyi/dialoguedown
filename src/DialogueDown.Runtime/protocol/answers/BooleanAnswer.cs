namespace DialogueDown.Runtime.Protocol;

/// <summary>
/// The world answers yes or no.
/// </summary>
/// <remarks>
/// What a guard is owed. A writer gates a line or a jump on a key — <c>Alice.HasKey</c>,
/// <c>QuestAccepted</c> — and the answer depends on what the player has done by the time the line
/// is reached. It cannot be settled when the script is compiled, which is why the running game is
/// asked for it.
/// <para>
/// A yes or a no decides one of two things: whether the node plays at all, when the key guards the
/// node, or whether one way out of it is taken, when the key guards an edge.
/// </para>
/// </remarks>
/// <param name="Holds">Whether the world says it is so.</param>
public sealed record BooleanAnswer(bool Holds) : Answer;

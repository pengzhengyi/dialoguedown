namespace DialogueDown.Runtime.Protocol;

/// <summary>
/// What was asked for has been carried out.
/// </summary>
/// <remarks>
/// The answer to a <see cref="Perform"/>, so a condition after an effect reads the world the effect
/// changed. One <c>Done</c> answers every <see cref="Perform"/> sent in the same step. It is
/// separate from <see cref="Next"/> so a host that skips through dialogue by sending
/// <see cref="Next"/> cannot skip past an effect.
/// </remarks>
public sealed record Done : Command;

namespace DialogueDown.Runtime.Protocol;

/// <summary>
/// What was asked for has been carried out.
/// </summary>
/// <remarks>
/// The answer to a <see cref="Perform"/>, and the reason a guard that follows an effect can trust
/// the world it reads. It is a separate command from <see cref="Next"/> on purpose: a host
/// fast-forwarding through dialogue may collapse the waits that are only presentation, and must
/// not be able to collapse this one.
/// </remarks>
public sealed record Done : Command;

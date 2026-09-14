namespace DialogueDown.Runtime.Protocol;

/// <summary>
/// Advance one step.
/// </summary>
/// <remarks>
/// The only way a run moves. Playing to a breakpoint, stepping over, and playing to the end are
/// all built by sending this repeatedly, so those policies live with whoever drives the run.
/// </remarks>
public sealed record Next : Command;

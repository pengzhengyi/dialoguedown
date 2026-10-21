namespace DialogueDown.Runtime.Protocol;

/// <summary>
/// The host drew this number.
/// </summary>
/// <remarks>
/// The answer to a <see cref="Draw"/>: a number from 0 to 1, both included, so a host can pass on
/// any common random source, whether or not it stops short of 1. A number out of range is refused
/// when the run takes it rather than when the command is made, because a number read from outside
/// can be anything.
/// </remarks>
/// <param name="Value">The number drawn, from 0 to 1.</param>
public sealed record Drawn(double Value) : Command;

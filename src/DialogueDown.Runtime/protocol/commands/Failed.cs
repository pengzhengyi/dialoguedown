namespace DialogueDown.Runtime.Protocol;

/// <summary>
/// What was asked for could not be carried out, and the run stands where it was.
/// </summary>
/// <remarks>
/// The other answer to a <see cref="Perform"/>. <see cref="Done"/> says the world changed; this
/// says it did not, so the run cannot read on and does not move. The driver may answer <see
/// cref="Done"/>, or give up, later. That is the driver's decision, not the runner's.
/// </remarks>
/// <param name="Explanation">Why the host could not carry it out, in the host's own words.</param>
public sealed record Failed(string Explanation) : Command;

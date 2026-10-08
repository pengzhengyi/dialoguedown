namespace DialogueDown.Runtime.Protocol;

/// <summary>
/// What was asked for could not be carried out, and the run stands where it was.
/// </summary>
/// <remarks>
/// The other answer to a <see cref="Perform"/>. The run stays where it was and sends no event;
/// the driver may later send <see cref="Done"/>, or stop.
/// </remarks>
/// <param name="Explanation">Why the host could not carry it out, in the host's own words.</param>
public sealed record Failed(string Explanation) : Command;

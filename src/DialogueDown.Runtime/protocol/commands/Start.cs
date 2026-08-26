namespace DialogueDown.Runtime.Protocol;

/// <summary>
/// Begin the run at the playbook's entry.
/// </summary>
/// <remarks>
/// Accepted wherever a run stands, so sending it again starts over: state is a value, and a fresh
/// one costs nothing to make. Later this carries an anchor, for beginning somewhere other than the
/// top.
/// </remarks>
public sealed record Start : Command;

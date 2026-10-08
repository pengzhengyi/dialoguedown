namespace DialogueDown.Runtime.Protocol;

/// <summary>
/// Begin the run at the playbook's entry.
/// </summary>
/// <remarks>
/// Accepted wherever a run stands, so sending it again starts over. It always begins at the entry;
/// beginning at an anchor is not supported.
/// </remarks>
public sealed record Start : Command;

namespace DialogueDown.Runtime.Situations;

/// <summary>
/// Standing at a menu, waiting for the player to take one of the options offered.
/// </summary>
/// <param name="Node">The menu's position in the playbook.</param>
public sealed record AwaitingChoice(int Node) : Situation;

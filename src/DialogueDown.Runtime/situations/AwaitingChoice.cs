namespace DialogueDown.Runtime.Situations;

/// <summary>
/// Standing at a menu, waiting for the player to take one of the options offered.
/// </summary>
/// <remarks>
/// Only the menu's position is kept. Its options are read from the node again when the player
/// chooses, so a wait cannot name an option its node does not have.
/// </remarks>
/// <param name="Node">The menu's position in the playbook.</param>
public sealed record AwaitingChoice(int Node) : Situation;

using System.Collections.Immutable;

namespace DialogueDown.Runtime.Situations;

/// <summary>
/// Standing at a menu, waiting for the player to take one of the options offered.
/// </summary>
/// <remarks>
/// The runner keeps nothing between steps, so the wait is where the run remembers which options it
/// offered as ones the player can take. An option offered as unavailable is then refused without
/// asking the world again. How many options there are, and where each leads, is read from the node
/// when the player chooses; the wait only says which of them the player may take.
/// </remarks>
/// <param name="Node">The menu's position in the playbook.</param>
/// <param name="Available">
/// The positions of the options offered as ones the player can take, in ascending order.
/// </param>
public sealed record AwaitingChoice(int Node, ImmutableArray<int> Available) : Situation;

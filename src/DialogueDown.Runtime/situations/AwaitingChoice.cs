using System.Collections.Immutable;

namespace DialogueDown.Runtime.Situations;

/// <summary>
/// Standing at a menu, waiting for the player to take one of the options offered.
/// </summary>
/// <remarks>
/// The positions of the options the player could take are kept from the offer, so an option offered
/// as unavailable is refused without asking the world again. How many options there are is read
/// from the node when the player chooses, so a wait cannot name an option its node does not have.
/// </remarks>
/// <param name="Node">The menu's position in the playbook.</param>
/// <param name="Available">
/// The positions of the options offered as ones the player can take, in ascending order.
/// </param>
public sealed record AwaitingChoice(int Node, ImmutableArray<int> Available) : Situation;

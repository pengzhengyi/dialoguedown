using System.Collections.Immutable;

namespace DialogueDown.Runtime.Situations;

/// <summary>
/// Standing at a node, waiting for the world to answer what it was asked.
/// </summary>
/// <remarks>
/// The run has reached the node and read what it needs, but cannot say what the node means until
/// the world replies. A guarded line waits here before anyone learns whether it is spoken at all.
/// <para>
/// The runner keeps nothing between steps, so the node and the moment are stored here. When the
/// <c>Supply</c> arrives, the runner reads the node again at that moment to learn what it asked,
/// and the supply must answer exactly that. The keys are kept as a record of which request is
/// open; the runner does not read them back.
/// </para>
/// </remarks>
/// <param name="Node">The node's position in the playbook.</param>
/// <param name="Keys">The keys the run asked the world about.</param>
/// <param name="Moment">
/// Where in the node the keys were asked: before playing, before a line continues after a command,
/// or before leaving.
/// </param>
public sealed record AwaitingSupply(
    int Node, ImmutableArray<string> Keys, Moment Moment) : Situation;

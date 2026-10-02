using System.Collections.Immutable;

namespace DialogueDown.Runtime.Situations;

/// <summary>
/// Standing at a node, waiting for the world to answer what it was asked.
/// </summary>
/// <remarks>
/// The run has reached the node and read what it needs, but cannot say what the node means until
/// the world replies. A guarded line waits here before anyone learns whether it is spoken at all.
/// <para>
/// The runner keeps nothing between steps, so the keys asked about are stored here, and the
/// <c>Supply</c> must answer exactly these keys. The moment says which point in the node the
/// answers are for.
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

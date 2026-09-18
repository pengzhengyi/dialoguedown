using System.Collections.Immutable;

namespace DialogueDown.Runtime.Situations;

/// <summary>
/// Standing at a node, waiting for the world to answer what it was asked.
/// </summary>
/// <remarks>
/// The run has reached the node and read what it needs, but cannot say what the node means until
/// the world replies. A guarded line waits here before anyone learns whether it is spoken at all.
/// <para>
/// The keys are kept here because nowhere else remembers them. The runner holds nothing between
/// steps, so when the answers arrive this is what they are held to: exactly these keys, no more
/// and no fewer.
/// </para>
/// </remarks>
/// <param name="Node">The node's position in the playbook.</param>
/// <param name="Keys">The keys the run asked the world about.</param>
public sealed record AwaitingSupply(int Node, ImmutableArray<string> Keys) : Situation;

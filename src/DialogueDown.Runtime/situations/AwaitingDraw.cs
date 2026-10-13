using System.Collections.Immutable;

namespace DialogueDown.Runtime.Situations;

/// <summary>
/// Standing at a random choice, waiting for the host to draw.
/// </summary>
/// <remarks>
/// The runner keeps nothing between steps, so the wait is where the run remembers the shares it
/// sent to the host. The host's draw is mapped onto those same shares, without asking the world
/// again. Where each arm leads is read from the node when the draw arrives, so the wait holds no
/// destination that could disagree with the node.
/// </remarks>
/// <param name="Node">The random choice's position in the playbook.</param>
/// <param name="Shares">Every arm's share, in the order written, as sent to the host.</param>
public sealed record AwaitingDraw(int Node, ImmutableArray<double> Shares) : Situation;

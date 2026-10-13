using System.Collections.Immutable;

namespace DialogueDown.Runtime.Situations;

/// <summary>
/// Standing at a random choice, waiting for the host to draw.
/// </summary>
/// <remarks>
/// The shares are kept from the draw request, so a draw is laid against the odds the host was
/// shown, without asking the world again. Where each arm leads is read from the node when the draw
/// arrives, so a wait cannot name an arm its node does not have.
/// </remarks>
/// <param name="Node">The random choice's position in the playbook.</param>
/// <param name="Shares">Every arm's share the draw request named, in the order written.</param>
public sealed record AwaitingDraw(int Node, ImmutableArray<double> Shares) : Situation;

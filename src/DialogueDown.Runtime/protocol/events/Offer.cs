using System.Collections.Immutable;

namespace DialogueDown.Runtime.Protocol;

/// <summary>
/// Offer the player these options, and say which one they take.
/// </summary>
/// <remarks>
/// A menu waits on the player, so it is a request, answered by the player's choice rather than by
/// <see cref="Next"/>.
/// <para>
/// A numbered menu is offered in the order the writer numbered its options, and is shown in that
/// order. A bulleted menu's order means nothing, so the host may shuffle what it shows.
/// </para>
/// </remarks>
/// <param name="Ordered">Whether the writer numbered the options, so they are shown in the order offered.</param>
/// <param name="Options">Every option of the menu, in the order offered.</param>
public sealed record Offer(bool Ordered, ImmutableArray<OfferedOption> Options) : Request;

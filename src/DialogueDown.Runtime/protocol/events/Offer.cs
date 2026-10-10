using System.Collections.Immutable;

namespace DialogueDown.Runtime.Protocol;

/// <summary>
/// Offer the player these options, and say which one they take.
/// </summary>
/// <remarks>
/// A menu waits on the player, so it is a request, answered by <see cref="Choose"/> rather than by
/// <see cref="Next"/>.
/// <para>
/// Every menu is offered in the order its options were written, so a position in the offer names
/// the same option in every runtime. A numbered menu is shown in that order; a bulleted menu may be
/// shown in any order, and a choice still names the option by its position here.
/// </para>
/// </remarks>
/// <param name="Ordered">Whether the writer numbered the options, so the host shows them in the order offered.</param>
/// <param name="Options">Every option of the menu, in the order written.</param>
public sealed record Offer(bool Ordered, ImmutableArray<OfferedOption> Options) : Request;

using System.Collections.Immutable;

namespace DialogueDown.Runtime.Protocol;

/// <summary>
/// Draw a number from 0 to 1, so the run can pick one arm of a random choice.
/// </summary>
/// <remarks>
/// A random choice picks an arm by weight, and the runner has no source of randomness, so it asks
/// the host for a draw and waits for a <see cref="Drawn"/>. The runner lays the shares end to end
/// in the order written and takes the arm whose span holds the draw times their total. With shares
/// <c>[0, 30, 20]</c>, a draw of <c>0.7</c> stands at 35 of the total of 50, inside the third
/// arm's span.
/// <para>
/// Every arm's share is named, so a host can show the odds, and a debugger that wants one arm
/// sends a draw inside that arm's span.
/// </para>
/// </remarks>
/// <param name="Shares">Every arm's share, in the order written. An arm with a share of zero is never drawn.</param>
public sealed record Draw(ImmutableArray<double> Shares) : Request;

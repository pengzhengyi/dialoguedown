using System.Collections.Immutable;

namespace DialogueDown.Runtime.Protocol;

/// <summary>
/// Draw a number from 0 to 1, so the run can pick one of a random choice's random options.
/// </summary>
/// <remarks>
/// A random choice picks a random option by weight, and the runner has no source of randomness, so
/// it asks the host for a draw and waits for a <see cref="Drawn"/>. The runner lays the shares end
/// to end in the order written and takes the random option whose span holds the draw times their
/// total. With shares <c>[0, 30, 20]</c>, a draw of <c>0.7</c> stands at 35 of the total of 50,
/// inside the third span.
/// <para>
/// Every random option's share is named, so a host can show the odds, and a debugger that wants
/// one random option sends a draw inside its span.
/// </para>
/// </remarks>
/// <param name="Shares">
/// Every random option's share, in the order written. A random option with a share of zero is never
/// drawn.
/// </param>
public sealed record Draw(ImmutableArray<double> Shares) : Request;

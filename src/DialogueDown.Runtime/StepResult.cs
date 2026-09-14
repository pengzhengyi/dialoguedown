using System.Collections.Immutable;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime;

/// <summary>
/// What one step produced: where the run now stands, and what it has to say.
/// </summary>
/// <remarks>
/// One step often has several things to report, and their order is part of what runtimes must
/// agree on: an effect is performed before the line that follows it, not merely alongside it.
/// </remarks>
/// <param name="State">The state after the step.</param>
/// <param name="Events">What the runner has to say, in the order it happened.</param>
public sealed record StepResult(PlayState State, ImmutableArray<Event> Events);

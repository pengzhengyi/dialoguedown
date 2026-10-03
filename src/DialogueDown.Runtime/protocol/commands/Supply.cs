using System.Collections.Immutable;

namespace DialogueDown.Runtime.Protocol;

/// <summary>
/// The driver's answers to a <see cref="Resolve"/>.
/// </summary>
/// <remarks>
/// Carries one answer for every key the request asked about and none it did not: a run asked
/// about <c>Alice.HasKey</c> is answered <c>{ "Alice.HasKey": BooleanAnswer(false) }</c>, and
/// anything else is refused.
/// <para>
/// The answers may come from a live game, a saved session, or a table of defaults; the runner
/// treats them alike, so the same script runs against a real game and against a preview with
/// nothing bound at all.
/// </para>
/// </remarks>
/// <param name="Answers">What the world says, by the key it was asked about.</param>
public sealed record Supply(ImmutableDictionary<string, Answer> Answers) : Command;

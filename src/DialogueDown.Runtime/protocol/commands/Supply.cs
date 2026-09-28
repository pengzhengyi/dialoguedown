using System.Collections.Immutable;

namespace DialogueDown.Runtime.Protocol;

/// <summary>
/// Here is what the world says.
/// </summary>
/// <remarks>
/// The answer to a <see cref="Resolve"/>, carrying one answer for every key it asked about and
/// none it did not — a run asked about <c>Alice.HasKey</c> is answered
/// <c>{ "Alice.HasKey": BooleanAnswer(false) }</c>, and anything else is refused.
/// <para>
/// Where the driver found the answers is its own business. A live game, a saved reading, and a
/// table of defaults all arrive here looking the same, which is what lets the same script run
/// against a real game and against a preview with nothing bound at all.
/// </para>
/// </remarks>
/// <param name="Answers">What the world says, by the key it was asked about.</param>
public sealed record Supply(ImmutableDictionary<string, Answer> Answers) : Command;

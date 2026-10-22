using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using DialogueDown.Conformance;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Tests.Conformance.Matchers;

/// <summary>Checks a <c>draw</c> claim: the shares the run asked the host to draw against.</summary>
/// <remarks>
/// The shares are compared in order and exactly, as numbers rather than as the text that writes
/// them. Every runtime works them out with the same arithmetic, so two runtimes given one playbook
/// and the same answers name the same shares.
/// </remarks>
internal sealed class DrawMatcher : IExpectationMatcher
{
    /// <inheritdoc/>
    public string Key => "draw";

    /// <inheritdoc/>
    public SessionOutcome Match(Event happened, JsonNode expected) =>
        happened is Draw draw
            ? MatchShares(draw.Shares, Shares(expected))
            : SessionOutcome.Diverged(
                $"expected the host to be asked for a draw, but the run {happened.Describe()}");

    private static SessionOutcome MatchShares(ImmutableArray<double> asked, ImmutableArray<double> expected) =>
        asked.SequenceEqual(expected)
            ? SessionOutcome.Conformed()
            : SessionOutcome.Diverged(
                $"expected a draw against {Written(expected)}, but the run asked for one against {Written(asked)}");

    // A claim names every share as a number, so anything else is a mistake in the fixture.
    private static ImmutableArray<double> Shares(JsonNode expected) =>
        expected is JsonArray shares && shares.All(share => share?.GetValueKind() == JsonValueKind.Number)
            ? [.. shares.Select(share => share!.GetValue<double>())]
            : throw new InvalidFixtureException("A draw claims every random option's share, as an array of numbers.");

    private static string Written(ImmutableArray<double> shares) =>
        JsonSerializer.Serialize(shares, FixtureJson.Compact);
}

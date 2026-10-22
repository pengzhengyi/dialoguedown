using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using DialogueDown.Conformance;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Tests.Conformance.Matchers;

/// <summary>Checks a <c>resolve</c> claim: what the run asked the world about.</summary>
/// <remarks>
/// A fixture names each kind of answer as a property, <c>boolean</c>, <c>text</c>, or
/// <c>number</c>, holding the keys asked for that kind, and leaves out a kind nothing asks for:
/// <c>{ "boolean": ["Alice.HasKey"], "text": ["playerName"] }</c>. Each kind is read by its name, so
/// the kinds may be written in any order. Each kind's keys are compared in order, because a run
/// names them as the playbook names them and two runtimes reading the same playbook therefore ask in
/// the same order.
/// </remarks>
internal sealed class ResolveMatcher : IExpectationMatcher
{
    private static readonly string[] _kinds = ["boolean", "text", "number"];

    /// <inheritdoc/>
    public string Key => "resolve";

    /// <inheritdoc/>
    public SessionOutcome Match(Event happened, JsonNode expected) =>
        happened is Resolve resolve
            ? MatchKeys(resolve, Claimed(expected))
            : SessionOutcome.Diverged(
                $"expected the world to be asked something, but the run {happened.Describe()}");

    private static SessionOutcome MatchKeys(Resolve resolve, JsonObject claimed) =>
        // Every kind is checked, so a key asked as the wrong kind is reported both where it was
        // asked and where it was claimed.
        SessionOutcome.Combine(
            MatchKind("a truth", resolve.BooleanKeys, KeysOf(claimed, "boolean")),
            MatchKind("words", resolve.TextKeys, KeysOf(claimed, "text")),
            MatchKind("a number", resolve.NumberKeys, KeysOf(claimed, "number")));

    private static SessionOutcome MatchKind(
        string answer, ImmutableArray<string> asked, ImmutableArray<string> claimed) =>
        asked.SequenceEqual(claimed, StringComparer.Ordinal)
            ? SessionOutcome.Conformed()
            : SessionOutcome.Diverged(
                $"expected {Written(claimed)} to be asked for {answer}, but {Written(asked)} was");

    // A kind is read by its name, so a misspelled one would otherwise go unread and leave the kind
    // it meant looking empty.
    private static JsonObject Claimed(JsonNode expected) =>
        expected is JsonObject claimed && claimed.All(kind => _kinds.Contains(kind.Key))
            ? claimed
            : throw new InvalidFixtureException(
                "A resolve claims its keys as an object naming boolean, text, or number.");

    // A kind the fixture leaves out is one the run asked nothing of.
    private static ImmutableArray<string> KeysOf(JsonObject claimed, string kind) =>
        claimed[kind] switch
        {
            null => [],
            JsonArray keys when keys.All(key => key?.GetValueKind() == JsonValueKind.String) =>
                [.. keys.Select(key => key!.GetValue<string>())],
            _ => throw new InvalidFixtureException($"A resolve claims its {kind} keys as an array of strings."),
        };

    private static string Written(ImmutableArray<string> keys) =>
        JsonSerializer.Serialize(keys, FixtureJson.Compact);
}

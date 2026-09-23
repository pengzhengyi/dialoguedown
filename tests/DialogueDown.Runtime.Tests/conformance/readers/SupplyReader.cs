using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using DialogueDown.Conformance;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Tests.Conformance.Readers;

/// <summary>Reads <c>supply</c>, which carries what the world says by the key it was asked about.</summary>
/// <remarks>
/// A truth is written as <c>true</c> or <c>false</c> and words are written as a string, so the
/// kind of answer a fixture gives is the kind JSON already wrote it as.
/// </remarks>
internal sealed class SupplyReader : ICommandReader
{
    /// <inheritdoc/>
    public string Key => "supply";

    /// <inheritdoc/>
    public Command Read(JsonNode? payload) =>
        payload is JsonObject answers
            ? new Supply(answers.ToImmutableDictionary(
                answer => answer.Key,
                answer => ReadAnswer(answer.Key, answer.Value),
                StringComparer.Ordinal))
            : throw new InvalidFixtureException("A supply send carries what the world says, by key.");

    private static Answer ReadAnswer(string key, JsonNode? written) =>
        written?.GetValueKind() switch
        {
            JsonValueKind.True or JsonValueKind.False => new BooleanAnswer(written.GetValue<bool>()),
            JsonValueKind.String => new TextAnswer(written.GetValue<string>()),
            _ => throw new InvalidFixtureException(
                $"The world answers {key} with a truth or with words, "
                    + $"not {written?.ToJsonString() ?? "nothing"}."),
        };
}

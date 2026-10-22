using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using DialogueDown.Conformance;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Tests.Conformance.Readers;

/// <summary>Reads <c>supply</c>, which carries what the world answered, by the key it was asked about.</summary>
/// <remarks>
/// A truth is written as <c>true</c> or <c>false</c>, words as a string, and a number as a number,
/// so the kind of answer a fixture gives is the kind JSON already wrote it as.
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
            : throw new InvalidFixtureException("A supply send carries what the world answered, by key.");

    private static Answer ReadAnswer(string key, JsonNode? written) =>
        written?.GetValueKind() switch
        {
            JsonValueKind.True or JsonValueKind.False => new BooleanAnswer(written.GetValue<bool>()),
            JsonValueKind.String => new TextAnswer(written.GetValue<string>()),
            JsonValueKind.Number => new NumberAnswer(written.GetValue<double>()),
            _ => throw new InvalidFixtureException(
                $"The world answers {key} with a truth, with words, or with a number, "
                    + $"not {written?.ToJsonString() ?? "nothing"}."),
        };
}

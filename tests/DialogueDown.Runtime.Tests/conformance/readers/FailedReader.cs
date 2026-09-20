using System.Text.Json;
using System.Text.Json.Nodes;
using DialogueDown.Conformance;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Tests.Conformance.Readers;

/// <summary>Reads <c>failed</c>, which carries the host's own words.</summary>
internal sealed class FailedReader : ICommandReader
{
    /// <inheritdoc/>
    public string Key => "failed";

    /// <inheritdoc/>
    public Command Read(JsonNode? payload) => new Failed(Words(payload));

    // The harness checks that they are words and nothing more: whether an explanation says enough
    // is the host's business, and the schema's. The wording is never asserted.
    private static string Words(JsonNode? payload) =>
        payload?.GetValueKind() == JsonValueKind.String
            ? payload.GetValue<string>()
            : throw new InvalidFixtureException("A failed send carries the host's explanation, as a string.");
}

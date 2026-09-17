using System.Text.Json.Nodes;
using DialogueDown.Conformance;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Tests.Conformance.Readers;

/// <summary>Reads <c>next</c>, which carries nothing.</summary>
internal sealed class NextReader : ICommandReader
{
    /// <inheritdoc/>
    public string Key => "next";

    /// <inheritdoc/>
    public Command Read(JsonNode? payload) =>
        payload is null
            ? new Next()
            : throw new InvalidFixtureException("A next send carries nothing.");
}

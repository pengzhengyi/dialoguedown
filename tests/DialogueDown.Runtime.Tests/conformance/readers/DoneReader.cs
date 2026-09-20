using System.Text.Json.Nodes;
using DialogueDown.Conformance;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Tests.Conformance.Readers;

/// <summary>Reads <c>done</c>, which carries nothing.</summary>
internal sealed class DoneReader : ICommandReader
{
    /// <inheritdoc/>
    public string Key => "done";

    /// <inheritdoc/>
    public Command Read(JsonNode? payload) =>
        payload is null
            ? new Done()
            : throw new InvalidFixtureException("A done send carries nothing.");
}

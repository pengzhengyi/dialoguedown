using System.Text.Json;
using System.Text.Json.Nodes;
using DialogueDown.Conformance;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Tests.Conformance.Readers;

/// <summary>Reads <c>drawn</c>, which carries the host's draw.</summary>
/// <remarks>
/// A draw outside 0 to 1 is taken as written, because whether the run can take it is the runner's
/// to say.
/// </remarks>
internal sealed class DrawnReader : ICommandReader
{
    /// <inheritdoc/>
    public string Key => "drawn";

    /// <inheritdoc/>
    public Command Read(JsonNode? payload) => new Drawn(Draw(payload));

    private static double Draw(JsonNode? payload) =>
        payload?.GetValueKind() == JsonValueKind.Number
            ? payload.GetValue<double>()
            : throw new InvalidFixtureException("A drawn send carries the host's draw, as a number.");
}

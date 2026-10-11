using System.Text.Json.Nodes;
using DialogueDown.Conformance;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Tests.Conformance.Readers;

/// <summary>Reads <c>choose</c>, which carries the position of the option taken.</summary>
/// <remarks>
/// A position counts from 0 among the options just offered. A position past the last option is
/// taken as written, because whether the menu has that option is the runner's to say.
/// </remarks>
internal sealed class ChooseReader : ICommandReader
{
    /// <inheritdoc/>
    public string Key => "choose";

    /// <inheritdoc/>
    public Command Read(JsonNode? payload) => new Choose(Position(payload));

    private static int Position(JsonNode? payload) =>
        payload is JsonValue value && value.TryGetValue<int>(out var position) && position >= 0
            ? position
            : throw new InvalidFixtureException(
                "A choose send carries the position of the option taken, as a whole number from 0.");
}

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace DialogueDown.Conformance;

/// <summary>
/// One entry in a session: something sent, or something the runtime must reply.
/// </summary>
/// <remarks>
/// The message stays as JSON rather than becoming a parallel set of C# types. The corpus is the
/// specification, and a second vocabulary standing beside it would be one more thing to keep in
/// step -- with nothing checking that it had been.
/// </remarks>
public abstract record SessionEntry
{
    private protected SessionEntry()
    {
    }
}

/// <summary>A message the driver sends.</summary>
/// <param name="Message">The command, in the corpus's own words.</param>
public sealed record Send(JsonNode Message) : SessionEntry;

/// <summary>A reply the runtime must produce next.</summary>
/// <param name="Message">The event, in the corpus's own words.</param>
public sealed record Expect(JsonNode Message) : SessionEntry;

internal sealed class SessionEntryJsonConverter : JsonConverter<SessionEntry>
{
    public override SessionEntry Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var exchange = JsonNode.Parse(ref reader)?.AsObject()
            ?? throw new JsonException("A session entry is an object holding one send or expect.");

        return (exchange["send"], exchange["expect"]) switch
        {
            (JsonNode sent, null) => new Send(sent),
            (null, JsonNode expected) => new Expect(expected),
            _ => throw new JsonException("A session entry is either sent or expected, never both."),
        };
    }

    public override void Write(Utf8JsonWriter writer, SessionEntry value, JsonSerializerOptions options) =>
        throw new NotSupportedException("A fixture's session entries are read, never written back out.");
}

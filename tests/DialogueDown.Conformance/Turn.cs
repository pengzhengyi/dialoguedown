using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace DialogueDown.Conformance;

/// <summary>
/// One exchange in a session: something sent, or something the runtime must reply.
/// </summary>
/// <remarks>
/// The message stays as JSON rather than becoming a parallel set of C# types. The corpus is the
/// specification, and a second vocabulary standing beside it would be one more thing to keep in
/// step -- with nothing checking that it had been.
/// </remarks>
public abstract record Turn
{
    private protected Turn()
    {
    }
}

/// <summary>A message the driver sends.</summary>
/// <param name="Message">The command, in the corpus's own words.</param>
public sealed record Send(JsonNode Message) : Turn;

/// <summary>A reply the runtime must produce next.</summary>
/// <param name="Message">The event, in the corpus's own words.</param>
public sealed record Expect(JsonNode Message) : Turn;

internal sealed class TurnJsonConverter : JsonConverter<Turn>
{
    public override Turn Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var exchange = JsonNode.Parse(ref reader)?.AsObject()
            ?? throw new JsonException("A turn is an object holding one send or expect.");

        return (exchange["send"], exchange["expect"]) switch
        {
            (JsonNode sent, null) => new Send(sent),
            (null, JsonNode expected) => new Expect(expected),
            _ => throw new JsonException("A turn is either sent or expected, never both."),
        };
    }

    public override void Write(Utf8JsonWriter writer, Turn value, JsonSerializerOptions options) =>
        throw new NotSupportedException("A fixture's turns are read, never written back out.");
}

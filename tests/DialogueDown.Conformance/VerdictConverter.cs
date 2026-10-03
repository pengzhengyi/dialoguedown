using System.Text.Json;
using System.Text.Json.Serialization;

namespace DialogueDown.Conformance;

/// <summary>
/// Reads and writes <see cref="Verdict"/> by its pinned wire names.
/// </summary>
/// <remarks>
/// Names only, so <c>"verdict": 1</c> is refused as the fixture schema refuses it. Hand-written
/// because the attribute that pins an enum member's name needs .NET 9, and this project also
/// targets net8.0.
/// </remarks>
internal sealed class VerdictConverter : JsonConverter<Verdict>
{
    /// <inheritdoc/>
    public override Verdict Read(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"A verdict is a string, not {reader.TokenType}.");
        }

        return reader.GetString() switch
        {
            "accept" => Verdict.Accept,
            "refuse" => Verdict.Refuse,
            var value => throw new JsonException($"'{value}' is not a verdict. Use accept or refuse."),
        };
    }

    /// <inheritdoc/>
    public override void Write(
        Utf8JsonWriter writer, Verdict value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value switch
        {
            Verdict.Accept => "accept",
            Verdict.Refuse => "refuse",
            _ => throw new ArgumentOutOfRangeException(
                nameof(value), value, "No wire name is defined for this verdict."),
        });
}

using System.Text.Json;
using System.Text.Json.Serialization;

namespace DialogueDown.Playbook.Speech;

/// <summary>
/// Reads and writes <see cref="SpeechStyle"/> by its pinned wire names.
/// </summary>
/// <remarks>
/// The names are part of the format contract, so they are written here as literals rather than
/// derived from the member names — a C# rename must not change what a playbook says. This is
/// hand-written because the alternative, <c>[JsonStringEnumMemberName]</c>, is a .NET 9 feature
/// that the net8.0 build could only get from an out-of-band System.Text.Json package. Only a
/// string is accepted: the schema disallows a number, and a reader is no more lenient than the
/// schema.
/// </remarks>
internal sealed class SpeechStyleConverter : JsonConverter<SpeechStyle>
{
    /// <inheritdoc/>
    public override SpeechStyle Read(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"A speech style is a string, not {reader.TokenType}.");
        }

        return reader.GetString() switch
        {
            "italic" => SpeechStyle.Italic,
            "bold" => SpeechStyle.Bold,
            "strikethrough" => SpeechStyle.Strikethrough,
            var value => throw new JsonException(
                $"'{value}' is not a speech style. Use italic, bold, or strikethrough."),
        };
    }

    /// <inheritdoc/>
    public override void Write(
        Utf8JsonWriter writer, SpeechStyle value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value switch
        {
            SpeechStyle.Italic => "italic",
            SpeechStyle.Bold => "bold",
            SpeechStyle.Strikethrough => "strikethrough",
            _ => throw new ArgumentOutOfRangeException(
                nameof(value), value, "No wire name is defined for this speech style."),
        });
}

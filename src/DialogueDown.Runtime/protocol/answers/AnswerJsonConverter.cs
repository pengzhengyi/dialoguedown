using System.Text.Json;
using System.Text.Json.Serialization;

namespace DialogueDown.Runtime.Protocol;

/// <summary>
/// Reads and writes an <see cref="Answer"/> as the bare JSON value it is.
/// </summary>
/// <remarks>
/// A supplied answer carries no tag naming its kind, because the JSON value already is one: a
/// driver answering <c>Alice.HasKey</c> writes <c>false</c>, and one answering <c>playerName</c>
/// writes <c>"Robin"</c>. Both read back as the kind that matches.
/// <code>
/// { "Alice.HasKey": false, "playerName": "Robin" }
/// </code>
/// <para>
/// The rule lives here so that everything reading a supply agrees on it — a fixture, a recorded
/// session, a driver on the far side of a socket. A value of any other kind is refused rather
/// than guessed at, because a number could be a weight or a count and nothing on the wire says
/// which.
/// </para>
/// </remarks>
public sealed class AnswerJsonConverter : JsonConverter<Answer>
{
    /// <summary>
    /// Gets a value indicating whether a written <c>null</c> reaches this converter.
    /// </summary>
    /// <remarks>
    /// It must. Left to itself the serializer turns a <c>null</c> into a missing answer without
    /// telling anyone, and a driver that answered a key with nothing would read as a driver that
    /// never answered it at all.
    /// </remarks>
    public override bool HandleNull => true;

    /// <inheritdoc />
    public override Answer Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.True or JsonTokenType.False => new TruthAnswer(reader.GetBoolean()),
            JsonTokenType.String => new TextAnswer(reader.GetString()!),
            var other => throw new JsonException(
                $"An answer is written as the value it is, so it must be a boolean or a string. "
                    + $"This one is {other}."),
        };

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Answer value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);

        switch (value)
        {
            case TruthAnswer truth:
                writer.WriteBooleanValue(truth.Holds);
                break;
            case TextAnswer text:
                writer.WriteStringValue(text.Value);
                break;
            default:
                throw new JsonException(
                    $"Nothing knows how to write a {value?.GetType().Name ?? "null"} as an answer.");
        }
    }
}

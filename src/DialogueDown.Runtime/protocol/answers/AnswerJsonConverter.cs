using System.Text.Json;
using System.Text.Json.Serialization;

namespace DialogueDown.Runtime.Protocol;

/// <summary>
/// Reads and writes an <see cref="Answer"/> as the bare JSON value it is.
/// </summary>
/// <remarks>
/// A supplied answer carries no tag naming its kind, because the JSON value already is one: a
/// driver answering <c>Alice.HasKey</c> writes <c>false</c>, one answering <c>playerName</c> writes
/// <c>"Robin"</c>, and one answering <c>Guard.Mood</c> writes <c>20</c>. Each reads back as the
/// kind that matches.
/// <code>
/// { "Alice.HasKey": false, "playerName": "Robin", "Guard.Mood": 20 }
/// </code>
/// <para>
/// The rule lives here so that everything reading a supply agrees on it — a fixture, a recorded
/// session, a driver on the far side of a socket. A value of any other kind is refused rather
/// than guessed at.
/// </para>
/// <para>
/// A number too large for a double, such as <c>1e400</c>, reads as infinity rather than failing,
/// so the run can refuse it as an invalid weight. JSON has no way to write infinity, so writing
/// one back is refused.
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
            JsonTokenType.True or JsonTokenType.False => new BooleanAnswer(reader.GetBoolean()),
            JsonTokenType.String => new TextAnswer(reader.GetString()!),
            JsonTokenType.Number => new NumberAnswer(reader.GetDouble()),
            var other => throw new JsonException(
                $"An answer is written as the value it is, so it must be a boolean, a string, or a "
                    + $"number. This one is {other}."),
        };

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Answer value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);

        switch (value)
        {
            case BooleanAnswer truth:
                writer.WriteBooleanValue(truth.Holds);
                break;
            case TextAnswer words:
                writer.WriteStringValue(words.Text);
                break;
            case NumberAnswer { Value: var number } when double.IsFinite(number):
                writer.WriteNumberValue(number);
                break;
            case NumberAnswer { Value: var number }:
                throw new JsonException($"JSON has no way to write the number {number}, which is not finite.");
            default:
                throw new JsonException(
                    $"Nothing knows how to write a {value?.GetType().Name ?? "null"} as an answer.");
        }
    }
}

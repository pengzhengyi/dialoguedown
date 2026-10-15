using System.Text.Json;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Tests.Protocol;

/// <summary>
/// What a supplied JSON value says, read the same way everywhere.
/// </summary>
/// <remarks>
/// A supplied answer carries no tag naming its kind — the JSON value is the tag, so a boolean
/// means a truth, a string means words, and a number means a number.
/// </remarks>
public sealed class AnswerJsonConverterTests
{
    private static readonly JsonSerializerOptions _options =
        new() { Converters = { new AnswerJsonConverter() } };

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    public void ABoolean_ReadsAsABooleanAnswer(string json, bool holds) =>
        AssertSays(Read(json), holds);

    [Theory]
    [InlineData("\"Robin\"", "Robin")]
    [InlineData("\"\"", "")]
    public void AString_ReadsAsATextAnswer(string json, string text) =>
        AssertSays(Read(json), text);

    [Theory]
    [InlineData("20", 20.0)]
    [InlineData("12.5", 12.5)]
    [InlineData("0", 0.0)]
    public void ANumber_ReadsAsANumberAnswer(string json, double value) =>
        AssertSays(Read(json), value);

    [Fact]
    public void ANumberPastWhatADoubleHolds_ReadsAsInfinity() =>
        // Read rather than refused, so the run can say the number is no weight, which a driver can
        // act on, rather than call the whole supply unreadable.
        AssertSays(Read("1e400"), double.PositiveInfinity);

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{}")]
    public void AnythingElse_IsRefused(string json) =>
        Assert.Throws<JsonException>(() => Read(json));

    [Theory]
    [InlineData(true, "true")]
    [InlineData(false, "false")]
    public void ABooleanAnswer_WritesBackAsABoolean(bool holds, string json) =>
        Assert.Equal(json, Write(new BooleanAnswer(holds)));

    [Theory]
    [InlineData("Robin", "\"Robin\"")]
    [InlineData("", "\"\"")]
    public void ATextAnswer_WritesBackAsAString(string text, string json) =>
        Assert.Equal(json, Write(new TextAnswer(text)));

    [Theory]
    [InlineData(20.0, "20")]
    [InlineData(12.5, "12.5")]
    public void ANumberAnswer_WritesBackAsANumber(double value, string json) =>
        Assert.Equal(json, Write(new NumberAnswer(value)));

    [Fact]
    public void ANumberAnswerNoJsonCanHold_IsRefusedOnTheWayOut() =>
        // JSON has no form for infinity, so writing one would make a document nothing can read.
        Assert.Throws<JsonException>(() => Write(new NumberAnswer(double.PositiveInfinity)));

    [Fact]
    public void AWholeSupply_ReadsKeyByKey()
    {
        // The shape a fixture actually carries: one object, answers of every kind inside it.
        var answers = ReadSupply("""{ "Alice.HasKey": false, "playerName": "Robin", "Guard.Mood": 20 }""");

        AssertSays(answers["Alice.HasKey"], false);
        AssertSays(answers["playerName"], "Robin");
        AssertSays(answers["Guard.Mood"], 20.0);
    }

    /// <summary>Asserts an answer is a yes or a no, and which.</summary>
    /// <param name="answer">The answer read.</param>
    /// <param name="holds">What it should say.</param>
    private static void AssertSays(Answer? answer, bool holds) =>
        Assert.Equal(holds, Assert.IsType<BooleanAnswer>(answer).Holds);

    /// <summary>Asserts an answer is words, and which.</summary>
    /// <param name="answer">The answer read.</param>
    /// <param name="text">What it should say.</param>
    private static void AssertSays(Answer? answer, string text) =>
        Assert.Equal(text, Assert.IsType<TextAnswer>(answer).Text);

    /// <summary>Asserts an answer is a number, and which.</summary>
    /// <param name="answer">The answer read.</param>
    /// <param name="value">What it should say.</param>
    private static void AssertSays(Answer? answer, double value) =>
        Assert.Equal(value, Assert.IsType<NumberAnswer>(answer).Value);

    private static Answer? Read(string json) => JsonSerializer.Deserialize<Answer>(json, _options);

    private static Dictionary<string, Answer> ReadSupply(string json) =>
        Assert.IsType<Dictionary<string, Answer>>(
            JsonSerializer.Deserialize<Dictionary<string, Answer>>(json, _options));

    private static string Write(Answer answer) => JsonSerializer.Serialize(answer, _options);
}

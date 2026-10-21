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
        AssertReadsAs(json, new BooleanAnswer(holds));

    [Theory]
    [InlineData("\"Robin\"", "Robin")]
    [InlineData("\"\"", "")]
    public void AString_ReadsAsATextAnswer(string json, string text) =>
        AssertReadsAs(json, new TextAnswer(text));

    [Theory]
    [InlineData("20", 20.0)]
    [InlineData("12.5", 12.5)]
    [InlineData("0", 0.0)]
    public void ANumber_ReadsAsANumberAnswer(string json, double value) =>
        AssertReadsAs(json, new NumberAnswer(value));

    [Fact]
    public void ANumberPastWhatADoubleHolds_ReadsAsInfinity() =>
        // Read rather than refused, so the run can say the number is no weight, which a driver can
        // act on, rather than call the whole supply unreadable.
        AssertReadsAs("1e400", new NumberAnswer(double.PositiveInfinity));

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{}")]
    public void AnythingElse_IsRefused(string json) =>
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Answer>(json, _options));

    [Theory]
    [InlineData(true, "true")]
    [InlineData(false, "false")]
    public void ABooleanAnswer_WritesBackAsABoolean(bool holds, string json) =>
        AssertWritesAs(new BooleanAnswer(holds), json);

    [Theory]
    [InlineData("Robin", "\"Robin\"")]
    [InlineData("", "\"\"")]
    public void ATextAnswer_WritesBackAsAString(string text, string json) =>
        AssertWritesAs(new TextAnswer(text), json);

    [Theory]
    [InlineData(20.0, "20")]
    [InlineData(12.5, "12.5")]
    public void ANumberAnswer_WritesBackAsANumber(double value, string json) =>
        AssertWritesAs(new NumberAnswer(value), json);

    [Fact]
    public void ANumberAnswerNoJsonCanHold_IsRefusedOnTheWayOut() =>
        // JSON has no form for infinity, so writing one would make a document nothing can read.
        Assert.Throws<JsonException>(
            () => JsonSerializer.Serialize<Answer>(new NumberAnswer(double.PositiveInfinity), _options));

    [Fact]
    public void AWholeSupply_ReadsKeyByKey()
    {
        // The shape a fixture actually carries: one object, answers of every kind inside it.
        var answers = ReadSupply("""{ "Alice.HasKey": false, "playerName": "Robin", "Guard.Mood": 20 }""");

        Assert.Equal(new BooleanAnswer(false), answers["Alice.HasKey"]);
        Assert.Equal(new TextAnswer("Robin"), answers["playerName"]);
        Assert.Equal(new NumberAnswer(20), answers["Guard.Mood"]);
    }

    /// <summary>Asserts a JSON value reads as the answer given: the same kind, saying the same.</summary>
    /// <param name="json">The JSON value.</param>
    /// <param name="expected">The answer it should read as.</param>
    private static void AssertReadsAs(string json, Answer expected) =>
        Assert.Equal(expected, JsonSerializer.Deserialize<Answer>(json, _options));

    /// <summary>Asserts an answer writes as the JSON value given.</summary>
    /// <param name="answer">The answer.</param>
    /// <param name="json">The JSON value it should write as.</param>
    private static void AssertWritesAs(Answer answer, string json) =>
        Assert.Equal(json, JsonSerializer.Serialize(answer, _options));

    private static Dictionary<string, Answer> ReadSupply(string json) =>
        Assert.IsType<Dictionary<string, Answer>>(
            JsonSerializer.Deserialize<Dictionary<string, Answer>>(json, _options));
}

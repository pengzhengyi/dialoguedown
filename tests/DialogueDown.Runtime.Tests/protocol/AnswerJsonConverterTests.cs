using System.Text.Json;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Tests.Protocol;

/// <summary>
/// What a supplied JSON value says, read the same way everywhere.
/// </summary>
/// <remarks>
/// A supplied answer carries no tag naming its kind — the JSON value is the tag, so a boolean
/// means a truth and a string means words. That rule has to live somewhere every reader can share,
/// or each one invents it and they drift.
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
    [InlineData("4")]
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

    [Fact]
    public void AWholeSupply_ReadsKeyByKey()
    {
        // The shape a fixture actually carries: one object, answers of both kinds inside it.
        var answers = ReadSupply("""{ "Alice.HasKey": false, "playerName": "Robin" }""");

        AssertSays(answers["Alice.HasKey"], false);
        AssertSays(answers["playerName"], "Robin");
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

    private static Answer? Read(string json) => JsonSerializer.Deserialize<Answer>(json, _options);

    private static Dictionary<string, Answer> ReadSupply(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, Answer>>(json, _options)!;

    private static string Write(Answer answer) => JsonSerializer.Serialize(answer, _options);
}

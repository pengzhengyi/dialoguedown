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
    public void ABoolean_ReadsAsATruth(string json, bool holds) =>
        Assert.Equal(holds, Assert.IsType<TruthAnswer>(Read(json)).Holds);

    [Theory]
    [InlineData("\"Robin\"", "Robin")]
    [InlineData("\"\"", "")]
    public void AString_ReadsAsWords(string json, string value) =>
        Assert.Equal(value, Assert.IsType<TextAnswer>(Read(json)).Value);

    [Theory]
    [InlineData("4")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{}")]
    public void AnythingElse_IsRefused(string json) =>
        Assert.Throws<JsonException>(() => Read(json));

    [Fact]
    public void AnAnswer_WritesBackAsTheValueItCameFrom()
    {
        Assert.Equal("true", JsonSerializer.Serialize<Answer>(new TruthAnswer(true), _options));
        Assert.Equal("\"Robin\"", JsonSerializer.Serialize<Answer>(new TextAnswer("Robin"), _options));
    }

    [Fact]
    public void AWholeSupply_ReadsKeyByKey()
    {
        // The shape a fixture actually carries: one object, answers of both kinds inside it.
        var answers = JsonSerializer.Deserialize<Dictionary<string, Answer>>(
            """{ "Alice.HasKey": false, "playerName": "Robin" }""", _options)!;

        Assert.False(Assert.IsType<TruthAnswer>(answers["Alice.HasKey"]).Holds);
        Assert.Equal("Robin", Assert.IsType<TextAnswer>(answers["playerName"]).Value);
    }

    private static Answer? Read(string json) => JsonSerializer.Deserialize<Answer>(json, _options);
}

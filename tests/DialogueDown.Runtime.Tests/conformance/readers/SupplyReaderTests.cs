using System.Text.Json.Nodes;
using DialogueDown.Conformance;
using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Tests.Conformance.Readers;

public sealed class SupplyReaderTests
{
    private readonly SupplyReader _reader = new();

    [Fact]
    public void Read_ATruth_IsABooleanAnswer() =>
        AssertReadAnswers("""{ "Alice.HasKey": false }""", ("Alice.HasKey", new BooleanAnswer(false)));

    [Fact]
    public void Read_Words_AreATextAnswer() =>
        AssertReadAnswers("""{ "playerName": "Robin" }""", ("playerName", new TextAnswer("Robin")));

    [Fact]
    public void Read_AnswersOfBothKindsTogether_KeepsEachToItsOwn() =>
        AssertReadAnswers(
            """{ "Alice.HasKey": true, "playerName": "Robin" }""",
            ("Alice.HasKey", new BooleanAnswer(true)),
            ("playerName", new TextAnswer("Robin")));

    [Fact]
    public void Read_AnEmptyObject_IsASupplyWithNoAnswers() => AssertReadAnswers("{ }");

    [Fact]
    public void Read_AnAnswerOfNeitherKind_IsAFixtureBug() =>
        AssertFixtureBug("""{ "Alice.HasKey": 3 }""");

    [Fact]
    public void Read_AKeyAnsweredWithNothing_IsAFixtureBug() =>
        AssertFixtureBug("""{ "Alice.HasKey": null }""");

    [Fact]
    public void Read_TheBareNameWithNothingBesideIt_IsAFixtureBug() =>
        // A supply always carries what the world answered, so there is no bare form of it.
        Assert.Throws<InvalidFixtureException>(() => _reader.Read(null));

    /// <summary>Asserts a supply send reads as exactly these answers, and no others.</summary>
    /// <param name="payload">What the send carries beside its key.</param>
    /// <param name="answers">Every answer the send should read as, by key.</param>
    private void AssertReadAnswers(string payload, params (string Key, Answer Answer)[] answers)
    {
        var read = Assert.IsType<Supply>(_reader.Read(JsonNode.Parse(payload))).Answers;

        Assert.Equal(answers.Length, read.Count);
        Assert.All(answers, answer => Assert.Equal(answer.Answer, read[answer.Key]));
    }

    /// <summary>Asserts a supply send gives the command a shape it cannot take.</summary>
    /// <param name="payload">What the send carries beside its key.</param>
    private void AssertFixtureBug(string payload) =>
        Assert.Throws<InvalidFixtureException>(() => _reader.Read(JsonNode.Parse(payload)));
}

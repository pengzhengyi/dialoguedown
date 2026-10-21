using System.Text.Json.Nodes;
using DialogueDown.Runtime.Protocol;
using static DialogueDown.Runtime.Tests.Conformance.Readers.CommandReaderAssert;

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
    public void Read_ANumber_IsANumberAnswer() =>
        AssertReadAnswers("""{ "Guard.Mood": 20 }""", ("Guard.Mood", new NumberAnswer(20)));

    [Fact]
    public void Read_AnswersOfEveryKindTogether_KeepsEachToItsOwn() =>
        AssertReadAnswers(
            """{ "Alice.HasKey": true, "playerName": "Robin", "Guard.Mood": 20 }""",
            ("Alice.HasKey", new BooleanAnswer(true)),
            ("playerName", new TextAnswer("Robin")),
            ("Guard.Mood", new NumberAnswer(20)));

    [Fact]
    public void Read_AnEmptyObject_IsASupplyWithNoAnswers() => AssertReadAnswers("{ }");

    [Fact]
    public void Read_AnAnswerOfNoKind_IsAFixtureBug() =>
        AssertRejected(_reader, """{ "Alice.HasKey": [] }""");

    [Fact]
    public void Read_AKeyAnsweredWithNothing_IsAFixtureBug() =>
        AssertRejected(_reader, """{ "Alice.HasKey": null }""");

    [Fact]
    public void Read_TheBareNameWithNothingBesideIt_IsAFixtureBug() =>
        // A supply always carries what the world answered, so there is no bare form of it.
        AssertRejected(_reader, payload: null);

    /// <summary>Asserts a supply send reads as exactly these answers, and no others.</summary>
    /// <param name="payload">What the send carries beside its key.</param>
    /// <param name="answers">Every answer the send should read as, by key.</param>
    private void AssertReadAnswers(string payload, params (string Key, Answer Answer)[] answers)
    {
        var read = Assert.IsType<Supply>(_reader.Read(JsonNode.Parse(payload))).Answers;

        Assert.Equal(answers.Length, read.Count);
        Assert.All(answers, answer => Assert.Equal(answer.Answer, read[answer.Key]));
    }
}

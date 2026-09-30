using System.Collections.Immutable;
using System.Text.Json.Nodes;
using DialogueDown.Conformance;
using DialogueDown.Playbook.Speech;
using DialogueDown.Runtime.Protocol;
using static DialogueDown.Runtime.Tests.Conformance.SessionOutcomeAssert;

namespace DialogueDown.Runtime.Tests.Conformance.Matchers;

public sealed class ContinuedMatcherTests
{
    private readonly ContinuedMatcher _matcher = new();

    [Fact]
    public void FlattenedSpeechThatAgreesConforms() =>
        AssertConformed(Match(WentOn(" Bye."), """{ "speech": " Bye." }"""));

    [Fact]
    public void SpeechClaimedAsFragmentsConformsWhenTheyAgree() =>
        AssertConformed(Match(WentOn(" Bye."), """{ "speech": [ { "kind": "text", "text": " Bye." } ] }"""));

    [Fact]
    public void DifferentSpeechDiverges() =>
        AssertDiverged(Match(WentOn(" Bye."), """{ "speech": " Farewell." }"""), "Farewell", "Bye");

    [Fact]
    public void ARunOpeningALineInsteadDiverges() =>
        // A port that opened a second line where the first went on would show the host two name
        // plates for one sentence.
        AssertDiverged(
            Match(new Said("Alice", [new TextFragment(" Bye.")]), """{ "speech": " Bye." }"""),
            "expected the line to go on",
            "heard Alice speak");

    [Fact]
    public void GoingOnIsDescribedWhenSomethingElseWasExpected() =>
        AssertDiverged(
            new EndedMatcher().Match(WentOn(" Bye."), JsonNode.Parse("{}")!),
            "heard the line go on");

    [Fact]
    public void AContinuedClaimingNoSpeechIsAnInvalidFixture() =>
        Assert.Throws<InvalidFixtureException>(() => Match(WentOn(" Bye."), "{}"));

    private static Continued WentOn(string text) =>
        new(ImmutableArray.Create<SpeechFragment>(new TextFragment(text)));

    private SessionOutcome Match(Event happened, string expected) =>
        _matcher.Match(happened, JsonNode.Parse(expected)!);
}

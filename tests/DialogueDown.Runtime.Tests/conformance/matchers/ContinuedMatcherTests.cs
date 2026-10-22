using DialogueDown.Conformance;
using DialogueDown.Runtime.Protocol;
using static DialogueDown.Runtime.Tests.Conformance.SessionEntries;
using static DialogueDown.Runtime.Tests.Conformance.SessionOutcomeAssert;
using static DialogueDown.Runtime.Tests.EventFactory;
using static DialogueDown.TestSupport.SpeechFactory;

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
        AssertDiverged(
            Match(WentOn(" Bye."), """{ "speech": " Farewell." }"""),
            """
            expected " Farewell.", but heard " Bye."
            """);

    [Fact]
    public void ARunOpeningALineInsteadDiverges() =>
        // A port that sent a second Said where the line went on would show the speaker's name
        // twice for one sentence.
        AssertDiverged(
            Match(Said("Alice", " Bye."), """{ "speech": " Bye." }"""),
            "expected the line to go on, but the run heard Alice speak");

    [Fact]
    public void GoingOnIsDescribedWhenSomethingElseWasExpected() =>
        AssertDiverged(
            new EndedMatcher().Match(WentOn(" Bye."), Json("{}")),
            "expected the run to end, but it heard the line go on");

    [Fact]
    public void AContinuedClaimingNoSpeechIsAnInvalidFixture() =>
        Assert.Throws<InvalidFixtureException>(() => Match(WentOn(" Bye."), "{}"));

    private static Continued WentOn(string text) =>
        new([Text(text)]);

    private SessionOutcome Match(Event happened, string expected) =>
        _matcher.Match(happened, Json(expected));
}

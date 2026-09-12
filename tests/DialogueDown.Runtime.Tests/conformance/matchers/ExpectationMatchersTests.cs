using System.Collections.Immutable;
using System.Text.Json.Nodes;
using DialogueDown.Conformance;
using DialogueDown.Playbook.Speech;
using DialogueDown.Runtime.Protocol;
using static DialogueDown.Runtime.Tests.Conformance.SessionEntries;
using static DialogueDown.Runtime.Tests.Conformance.SessionOutcomeAssert;

namespace DialogueDown.Runtime.Tests.Conformance.Matchers;

public sealed class ExpectationMatchersTests
{
    private const string SaidHi = """{ "said": { "speaker": "Alice", "speech": "Hi" } }""";

    private static readonly Said _hello =
        new("Alice", ImmutableArray.Create<SpeechFragment>(new TextFragment("Hi")));

    [Fact]
    public void AClaimThatHoldsConforms()
    {
        AssertConformed(Match(_hello, SaidHi));
    }

    [Fact]
    public void AClaimThatFailsDiverges()
    {
        AssertDiverged(
            Match(_hello, """{ "said": { "speaker": "Bob", "speech": "Hi" } }"""),
            "expected Bob to speak");
    }

    [Fact]
    public void EveryMatcherOwnsItsOwnKey()
    {
        AssertConformed(Match(new Ended(), """{ "ended": true }"""));
    }

    [Fact]
    public void AClaimNobodyOwnsIsNotYetRunnable()
    {
        AssertNotYetRunnable(Match(_hello, """{ "resolve": [ "Alice.HasKey" ] }"""), "resolve");
    }

    [Fact]
    public void AnUncheckedClaimBesideOneThatHoldsDoesNotRideAlong()
    {
        // Stopping at the first claim recognized would report this as held, having read half of it.
        AssertNotYetRunnable(
            Match(_hello, """{ "said": { "speaker": "Alice", "speech": "Hi" }, "resolve": [ "Alice.HasKey" ] }"""),
            "resolve");
    }

    [Fact]
    public void ARealDivergenceBesideAnUncheckedClaimStillDiverges()
    {
        AssertDiverged(
            Match(_hello, """{ "said": { "speaker": "Bob", "speech": "Hi" }, "resolve": [ "Alice.HasKey" ] }"""),
            "expected Bob to speak");
    }

    [Fact]
    public void AnExpectationClaimingNothingIsAnInvalidFixture()
    {
        Assert.Throws<InvalidFixtureException>(() => Match(_hello, "{}"));
    }

    [Fact]
    public void AClaimSayingNothingIsAnInvalidFixture()
    {
        Assert.Throws<InvalidFixtureException>(() => Match(_hello, """{ "said": null }"""));
    }

    [Fact]
    public void ARunThatHasFallenSilentDiverges()
    {
        AssertDiverged(
            ExpectationMatchers.Match(null, Expected(SaidHi)),
            "the run fell silent",
            "Alice");
    }

    private static SessionOutcome Match(Event happened, string expectation) =>
        ExpectationMatchers.Match(happened, JsonNode.Parse(expectation)!.AsObject());
}

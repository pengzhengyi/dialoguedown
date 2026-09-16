using System.Text.Json.Nodes;
using DialogueDown.Conformance;
using DialogueDown.Runtime.Protocol;
using static DialogueDown.Runtime.Tests.Conformance.SessionOutcomeAssert;

namespace DialogueDown.Runtime.Tests.Conformance.Matchers;

public sealed class RefusedMatcherTests
{
    private readonly RefusedMatcher _matcher = new();

    [Fact]
    public void ARefusalForTheReasonClaimedConforms()
    {
        AssertConformed(Match(Refusing(RefusalReason.AlreadyEnded), """{ "reason": "already-ended" }"""));
    }

    [Fact]
    public void ARefusalForAnotherReasonDiverges()
    {
        AssertDiverged(
            Match(Refusing(RefusalReason.NotStarted), """{ "reason": "already-ended" }"""),
            "expected a refusal for already-ended",
            "refused for not-started");
    }

    [Fact]
    public void SomethingOtherThanARefusalDiverges()
    {
        AssertDiverged(
            Match(new Ended(), """{ "reason": "already-ended" }"""),
            "expected the run to refuse",
            "ended");
    }

    [Fact]
    public void AClaimWithNoReasonIsAFixtureBug()
    {
        Assert.Throws<InvalidFixtureException>(
            () => Match(Refusing(RefusalReason.AlreadyEnded), "{}"));
    }

    [Fact]
    public void AReasonTheProtocolDoesNotNameIsAFixtureBug()
    {
        Assert.Throws<InvalidFixtureException>(
            () => Match(Refusing(RefusalReason.AlreadyEnded), """{ "reason": "because-i-said-so" }"""));
    }

    private static Refused Refusing(RefusalReason reason) => new(reason, "The run is over.");

    private SessionOutcome Match(Event happened, string claimed) =>
        _matcher.Match(happened, JsonNode.Parse(claimed)!);
}

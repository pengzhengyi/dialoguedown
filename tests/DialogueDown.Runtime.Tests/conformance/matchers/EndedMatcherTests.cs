using System.Text.Json.Nodes;
using DialogueDown.Runtime.Protocol;
using static DialogueDown.Runtime.Tests.Conformance.SessionOutcomeAssert;
using static DialogueDown.TestSupport.SpeechFactory;

namespace DialogueDown.Runtime.Tests.Conformance.Matchers;

public sealed class EndedMatcherTests
{
    private readonly EndedMatcher _matcher = new();

    [Fact]
    public void AnEndedRunConforms()
    {
        AssertConformed(Match(new Ended()));
    }

    [Fact]
    public void ARunStillSpeakingDiverges()
    {
        AssertDiverged(Match(Spoke("Alice")), "expected the run to end", "heard Alice speak");
    }

    [Fact]
    public void ARefusalDiverges()
    {
        AssertDiverged(Match(new Refused(RefusalReason.LeadsNowhere, "no step")), "expected the run to end", "refused: no step");
    }

    [Fact]
    public void AnUnnamedSpeakerIsDescribedAsTheDefaultOne()
    {
        AssertDiverged(Match(Spoke(null)), "heard <default> speak");
    }

    private static Said Spoke(string? speaker) =>
        new(speaker, [Text("Hi")]);

    // What an `ended` claim carries says nothing beyond the key itself, so the tests do not read it.
    private SessionOutcome Match(Event happened) => _matcher.Match(happened, JsonNode.Parse("true")!);
}

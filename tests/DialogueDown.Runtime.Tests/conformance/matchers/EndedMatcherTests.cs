using System.Collections.Immutable;
using System.Text.Json.Nodes;
using DialogueDown.Playbook.Speech;
using DialogueDown.Runtime.Protocol;
using static DialogueDown.Runtime.Tests.Conformance.SessionOutcomeAssert;

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
        AssertDiverged(Match(new Refused("no step")), "expected the run to end", "refused: no step");
    }

    [Fact]
    public void AnUnnamedSpeakerIsDescribedAsTheDefaultOne()
    {
        AssertDiverged(Match(Spoke(null)), "heard <default> speak");
    }

    private static Said Spoke(string? speaker) =>
        new(speaker, ImmutableArray.Create<SpeechFragment>(new TextFragment("Hi")));

    // What an `ended` claim carries says nothing beyond the key itself, so the tests do not read it.
    private SessionOutcome Match(Event happened) => _matcher.Match(happened, JsonNode.Parse("true")!);
}

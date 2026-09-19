using System.Collections.Immutable;
using System.Text.Json.Nodes;
using DialogueDown.Conformance;
using DialogueDown.Playbook.Speech;
using DialogueDown.Runtime.Protocol;
using static DialogueDown.Runtime.Tests.Conformance.SessionOutcomeAssert;

namespace DialogueDown.Runtime.Tests.Conformance.Matchers;

public sealed class SaidMatcherTests
{
    private const string Fragments = """{ "speaker": "Alice", "speech": [ { "kind": "text", "text": "Hi" } ] }""";

    private readonly SaidMatcher _matcher = new();

    [Fact]
    public void ASpeakerAndFlattenedSpeechThatAgreeConform()
    {
        AssertConformed(Match(Spoke("Alice", "Hi"), """{ "speaker": "Alice", "speech": "Hi" }"""));
    }

    [Fact]
    public void FlattenedSpeechReadsTheWordsInsideAStyledRun()
    {
        AssertConformed(Match(
            new Said("Alice", [
                new TextFragment("My key is "),
                new StyledTextFragment(SpeechStyle.Bold, [new TextFragment("rusty")]),
                new TextFragment("."),
            ]),
            """{ "speaker": "Alice", "speech": "My key is rusty." }"""));
    }

    [Fact]
    public void SpeechClaimedAsFragmentsConformsWhenTheyAgree()
    {
        AssertConformed(Match(Spoke("Alice", "Hi"), Fragments));
    }

    [Fact]
    public void AnExpectationNamingNobodyMatchesTheUnnamedSpeaker()
    {
        AssertConformed(Match(Spoke(null, "Hi"), """{ "speech": "Hi" }"""));
    }

    [Fact]
    public void ADifferentSpeakerDiverges()
    {
        AssertDiverged(
            Match(Spoke("Alice", "Hi"), """{ "speaker": "Bob", "speech": "Hi" }"""),
            "expected Bob to speak, but Alice did");
    }

    [Fact]
    public void TheUnnamedSpeakerIsNamedAsTheDefaultOne()
    {
        AssertDiverged(
            Match(Spoke(null, "Hi"), """{ "speaker": "Alice", "speech": "Hi" }"""),
            "expected Alice to speak, but <default> did");
    }

    [Fact]
    public void DifferentPlainTextSpeechDiverges()
    {
        AssertDiverged(
            Match(Spoke("Alice", "Hello"), """{ "speaker": "Alice", "speech": "Hi" }"""),
            "expected \"Hi\", but heard \"Hello\"");
    }

    [Fact]
    public void DifferentFragmentSpeechDiverges()
    {
        AssertDiverged(Match(Spoke("Alice", "Hello"), Fragments), "fragment 0", "Hello", "Hi");
    }

    [Fact]
    public void WhenBothTheSpeakerAndTheSpeechDiffer_BothAreReported()
    {
        // Combining gathers reasons of the same graveness, so one run names both faults.
        var outcome = Match(Spoke("Alice", "Hello"), """{ "speaker": "Bob", "speech": "Hi" }""");

        AssertDiverged(
            outcome,
            "expected Bob to speak, but Alice did",
            "expected \"Hi\", but heard \"Hello\"");
        Assert.Equal(2, outcome.Reasons.Length);
    }

    [Fact]
    public void AnythingButSpeechDiverges()
    {
        AssertDiverged(
            Match(new Ended(), """{ "speaker": "Alice", "speech": "Hi" }"""),
            "expected somebody to speak",
            "ended");
    }

    [Fact]
    public void ASaidClaimingNoSpeechIsAnInvalidFixture()
    {
        Assert.Throws<InvalidFixtureException>(() => Match(Spoke("Alice", "Hi"), """{ "speaker": "Alice" }"""));
    }

    [Fact]
    public void FragmentsSpelledInAnotherOrderStillConform()
    {
        // A fixture may write a fragment's properties in any order; the speech is the same.
        AssertConformed(Match(
            new Said("Alice", [new StyledTextFragment(SpeechStyle.Bold, [new TextFragment("Hi")])]),
            """{ "speaker": "Alice", "speech": [ { "kind": "styled", "children": [ { "kind": "text", "text": "Hi" } ], "style": "bold" } ] }"""));
    }

    [Fact]
    public void FewerFragmentsThanClaimedDiverges()
    {
        AssertDiverged(
            Match(Spoke("Alice", "Hi"), """{ "speaker": "Alice", "speech": [ { "kind": "text", "text": "Hi" }, { "kind": "break" } ] }"""),
            "expected 2 fragments, but heard 1");
    }

    [Fact]
    public void ADifferingFragmentIsNamedByItsPlace()
    {
        AssertDiverged(
            Match(
                new Said("Alice", [new TextFragment("Hi"), new TextFragment("there")]),
                """{ "speaker": "Alice", "speech": [ { "kind": "text", "text": "Hi" }, { "kind": "text", "text": "friend" } ] }"""),
            "fragment 1",
            "friend",
            "there");
    }

    [Fact]
    public void TwoDifferingFragments_AreBothReported()
    {
        // The counts agree, so every fragment is compared and every mismatch is named.
        var outcome = Match(
            new Said("Alice", [new TextFragment("Hi"), new TextFragment("there")]),
            """{ "speaker": "Alice", "speech": [ { "kind": "text", "text": "Hey" }, { "kind": "text", "text": "friend" } ] }""");

        AssertDiverged(outcome, "fragment 0", "Hey", "fragment 1", "friend");
        Assert.Equal(2, outcome.Reasons.Length);
    }

    [Fact]
    public void SpeechThatIsNotSpeechIsAnInvalidFixture()
    {
        Assert.Throws<InvalidFixtureException>(
            () => Match(Spoke("Alice", "Hi"), """{ "speaker": "Alice", "speech": [ { "kind": "nonsense" } ] }"""));
    }

    private static Said Spoke(string? speaker, string text) =>
        new(speaker, ImmutableArray.Create<SpeechFragment>(new TextFragment(text)));

    private SessionOutcome Match(Event happened, string expectation) =>
        _matcher.Match(happened, JsonNode.Parse(expectation)!);
}

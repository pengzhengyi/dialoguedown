using DialogueDown.Conformance;
using DialogueDown.Runtime.Protocol;
using static DialogueDown.Runtime.Tests.Conformance.SessionEntries;
using static DialogueDown.Runtime.Tests.Conformance.SessionOutcomeAssert;
using static DialogueDown.Runtime.Tests.EventFactory;
using static DialogueDown.TestSupport.SpeechFactory;

namespace DialogueDown.Runtime.Tests.Conformance.Matchers;

public sealed class SaidMatcherTests
{
    private const string Fragments = """{ "speaker": "Alice", "speech": [ { "kind": "text", "text": "Hi" } ] }""";

    private readonly SaidMatcher _matcher = new();

    [Fact]
    public void ASpeakerAndFlattenedSpeechThatAgreeConform()
    {
        AssertConformed(Match(Said("Alice", "Hi"), """{ "speaker": "Alice", "speech": "Hi" }"""));
    }

    [Fact]
    public void FlattenedSpeechReadsTheWordsInsideAStyledRun()
    {
        AssertConformed(Match(
            Said("Alice", Text("My key is "), Bold("rusty"), Text(".")),
            """{ "speaker": "Alice", "speech": "My key is rusty." }"""));
    }

    [Fact]
    public void SpeechClaimedAsFragmentsConformsWhenTheyAgree()
    {
        AssertConformed(Match(Said("Alice", "Hi"), Fragments));
    }

    [Fact]
    public void AnExpectationNamingNobodyMatchesTheUnnamedSpeaker()
    {
        AssertConformed(Match(Said(null, "Hi"), """{ "speech": "Hi" }"""));
    }

    [Fact]
    public void ADifferentSpeakerDiverges()
    {
        AssertDiverged(
            Match(Said("Alice", "Hi"), """{ "speaker": "Bob", "speech": "Hi" }"""),
            "expected Bob to speak, but Alice did");
    }

    [Fact]
    public void TheUnnamedSpeakerIsNamedAsTheDefaultOne()
    {
        AssertDiverged(
            Match(Said(null, "Hi"), """{ "speaker": "Alice", "speech": "Hi" }"""),
            "expected Alice to speak, but <default> did");
    }

    [Fact]
    public void DifferentPlainTextSpeechDiverges()
    {
        AssertDiverged(
            Match(Said("Alice", "Hello"), """{ "speaker": "Alice", "speech": "Hi" }"""),
            """
            expected "Hi", but heard "Hello"
            """);
    }

    [Fact]
    public void PlainTextSpeechDifferingInAnUnseenCharacter_DivergesNamingIt()
    {
        AssertDiverged(
            Match(Said("Alice", "a\u00A0b\u200B"), """{ "speaker": "Alice", "speech": "a\tb" }"""),
            """
            expected "a\u0009b", but heard "a\u00A0b\u200B"
            """);
    }

    [Fact]
    public void DifferentFragmentSpeechDiverges()
    {
        AssertDiverged(
            Match(Said("Alice", "Hello"), Fragments),
            """fragment 0: expected {"kind":"text","text":"Hi"}, but heard {"kind":"text","text":"Hello"}""");
    }

    [Fact]
    public void WhenBothTheSpeakerAndTheSpeechDiffer_BothAreReported()
    {
        // Combining gathers reasons of the same graveness, so one run names both faults.
        var outcome = Match(Said("Alice", "Hello"), """{ "speaker": "Bob", "speech": "Hi" }""");

        AssertDiverged(
            outcome,
            "expected Bob to speak, but Alice did",
            """
            expected "Hi", but heard "Hello"
            """);
    }

    [Fact]
    public void AnythingButSpeechDiverges()
    {
        AssertDiverged(
            Match(new Ended(), """{ "speaker": "Alice", "speech": "Hi" }"""),
            "expected somebody to speak, but the run ended");
    }

    [Fact]
    public void ASaidClaimingNoSpeechIsAnInvalidFixture()
    {
        Assert.Throws<InvalidFixtureException>(() => Match(Said("Alice", "Hi"), """{ "speaker": "Alice" }"""));
    }

    [Fact]
    public void FragmentsSpelledInAnotherOrderStillConform()
    {
        // A fixture may write a fragment's properties in any order; the speech is the same.
        AssertConformed(Match(
            Said("Alice", Bold("Hi")),
            """{ "speaker": "Alice", "speech": [ { "kind": "styled", "children": [ { "kind": "text", "text": "Hi" } ], "style": "bold" } ] }"""));
    }

    [Fact]
    public void FewerFragmentsThanClaimedDiverges()
    {
        AssertDiverged(
            Match(Said("Alice", "Hi"), """{ "speaker": "Alice", "speech": [ { "kind": "text", "text": "Hi" }, { "kind": "break" } ] }"""),
            "expected 2 fragments, but heard 1");
    }

    [Fact]
    public void ADifferingFragmentIsNamedByItsPlace()
    {
        AssertDiverged(
            Match(
                Said("Alice", Text("Hi"), Text("there")),
                """{ "speaker": "Alice", "speech": [ { "kind": "text", "text": "Hi" }, { "kind": "text", "text": "friend" } ] }"""),
            """fragment 1: expected {"kind":"text","text":"friend"}, but heard {"kind":"text","text":"there"}""");
    }

    [Fact]
    public void TwoDifferingFragments_AreBothReported()
    {
        // The counts agree, so every fragment is compared and every mismatch is named.
        var outcome = Match(
            Said("Alice", Text("Hi"), Text("there")),
            """{ "speaker": "Alice", "speech": [ { "kind": "text", "text": "Hey" }, { "kind": "text", "text": "friend" } ] }""");

        AssertDiverged(
            outcome,
            """fragment 0: expected {"kind":"text","text":"Hey"}, but heard {"kind":"text","text":"Hi"}""",
            """fragment 1: expected {"kind":"text","text":"friend"}, but heard {"kind":"text","text":"there"}""");
    }

    [Fact]
    public void SpeechThatIsNotSpeechIsAnInvalidFixture()
    {
        Assert.Throws<InvalidFixtureException>(
            () => Match(Said("Alice", "Hi"), """{ "speaker": "Alice", "speech": [ { "kind": "nonsense" } ] }"""));
    }

    private SessionOutcome Match(Event happened, string expectation) =>
        _matcher.Match(happened, Json(expectation));
}

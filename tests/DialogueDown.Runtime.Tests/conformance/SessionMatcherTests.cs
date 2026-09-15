using DialogueDown.Conformance;
using static DialogueDown.Runtime.Tests.Conformance.SessionEntries;
using static DialogueDown.Runtime.Tests.Conformance.SessionOutcomeAssert;

namespace DialogueDown.Runtime.Tests.Conformance;

public sealed class SessionMatcherTests
{
    [Fact]
    public void ASessionTheRunnerAnswersInFullConforms()
    {
        AssertConformed(Match(
            Playbooks.TwoLines(),
            Expected("""{ "said": { "speaker": "Alice", "speech": "Hello." } }"""),
            SentCommand("next"),
            Expected("""{ "said": { "speaker": "Bob", "speech": "Goodbye." } }"""),
            SentCommand("next"),
            Expected("""{ "ended": true }""")));
    }

    [Fact]
    public void AnExpectationTheRunnerDoesNotMeetDiverges()
    {
        AssertDiverged(
            Match(Playbooks.TwoLines(), Expected("""{ "said": { "speaker": "Bob", "speech": "Hello." } }""")),
            "expected Bob to speak, but Alice did");
    }

    [Fact]
    public void AnExpectationPastTheLastEventDiverges()
    {
        AssertDiverged(
            Match(
                Playbooks.OneLine(),
                Expected("""{ "said": { "speaker": "Alice", "speech": "Hello." } }"""),
                Expected("""{ "ended": true }""")),
            "the run fell silent",
            "ended");
    }

    [Fact]
    public void EventsTheSessionNeverReadsDiverge()
    {
        // The session stops early, leaving the opening line unread.
        AssertDiverged(Match(Playbooks.OneLine()), "the session ended, but the run still has 1 event unmatched");
    }

    [Fact]
    public void ASendNothingPlaysYetIsNotYetRunnable()
    {
        AssertNotYetRunnable(Match(Playbooks.OneLine(), SentCommand("frobnicate")), "frobnicate");
    }

    [Fact]
    public void ASessionThatBeginsItselfIsNotStartedForIt()
    {
        var op = new SessionOperator(Playbooks.OneLine());

        SessionMatcher.Match(op, [Sent("""{ "start": {} }""")]);

        // Starting it anyway would have queued the opening line before the session asked for it.
        Assert.Equal(0, op.UnreadEventCount);
    }

    private static SessionOutcome Match(PlayContext context, params SessionEntry[] session) =>
        SessionMatcher.Match(new SessionOperator(context), [.. session]);

}

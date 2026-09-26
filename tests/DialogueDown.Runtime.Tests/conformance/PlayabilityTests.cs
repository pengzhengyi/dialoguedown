using System.Collections.Immutable;
using DialogueDown.Conformance;
using DialogueDown.Playbook.Nodes;
using DialogueDown.TestSupport;
using static DialogueDown.Runtime.Tests.Conformance.PlayabilityAssert;
using static DialogueDown.Runtime.Tests.Conformance.SessionEntries;
using static DialogueDown.Runtime.Tests.PlaybookNodes;

namespace DialogueDown.Runtime.Tests.Conformance;

public sealed class PlayabilityTests
{
    [Fact]
    public void CanPlay_ASendSomeReaderOwns_Is()
    {
        AssertPlayable(SentCommand("next"));
    }

    [Fact]
    public void CanPlay_ASendNoReaderOwns_IsNot()
    {
        AssertNotPlayable(Sent("""{ "choose": 0 }"""));
    }

    [Fact]
    public void CanPlay_AnExpectation_Is()
    {
        // Its claims are checked where they are read, so the entry itself is always takeable.
        AssertPlayable(Expected("""{ "ended": true }"""));
    }

    [Fact]
    public void CanPlay_AnExpectationNobodyCanCheck_IsNot()
    {
        // An entry is only as takeable as the claims in it: a key no matcher owns ends the run
        // where it is read, so the validation detects before the run starts.
        AssertNotPlayable(Expected("""{ "asked": [] }"""));
    }

    [Fact]
    public void WhyNotPlayable_OfAPlaybook_NamesEachKindOnce()
    {
        var context = PlayContextFactory.Of(
            [Choice(0, leadsTo: 3), Choice(1, leadsTo: 3), RandomChoice(2, leadsTo: 3), End(3)],
            ["Alice"]);

        AssertNotPlayable(context, "nothing plays a ChoiceNode yet", "nothing plays a RandomChoiceNode yet");
    }

    [Fact]
    public void WhyNotPlayable_OfASendNoReaderOwns_NamesItsCommand()
    {
        AssertNotPlayable(Sent("""{ "choose": 0 }"""), "nothing sends choose yet");
    }

    [Fact]
    public void WhyNotPlayable_OfWhatCanBeTaken_IsEmpty()
    {
        AssertPlayable(SentCommand("next"));
        AssertPlayable(Expected("""{ "ended": true }"""));
    }

    [Fact]
    public void WhyNotPlayable_OfAnExpectation_NamesEachClaimNobodyChecks()
    {
        var session = ImmutableArray.Create<SessionEntry>(
            Expected("""{ "asked": [ { "label": "Go east" } ], "frobnicate": true }"""));

        AssertNotPlayable(session, "nothing checks asked yet", "nothing checks frobnicate yet");
    }

    [Fact]
    public void WhyNotPlayable_OfASession_NamesEachUnreadSendOnce()
    {
        var session = ImmutableArray.Create<SessionEntry>(
            SentCommand("next"),
            SentCommand("frobnicate"),
            SentCommand("frobnicate"),
            Sent("""{ "choose": 0 }"""),
            Sent("""{ "choose": 1 }"""));

        AssertNotPlayable(session, "nothing sends frobnicate yet", "nothing sends choose yet");
    }

    [Fact]
    public void WhatTheHarnessDeclines_IsWhatTheRunnerRefuses()
    {
        // The harness screens a playbook before stepping, so a construct nobody has taught reads
        // as untaught rather than as a divergence. That screen states what this build can play a
        // second time, and the two must agree: a case would otherwise be reported as untaught
        // while it plays, or as a divergence when nobody had taught it.
        foreach (var node in OneOfEveryNodeKind())
        {
            var refused = node.RefusalOnArrival() is not null;

            Assert.True(
                Playability.CanPlay(node) == !refused,
                $"{node.GetType().Name}: the harness calls it "
                    + $"{(Playability.CanPlay(node) ? "playable" : "untaught")}, "
                    + $"but arriving at one {(refused ? "refuses" : "does not refuse")}.");
        }
    }

    [Fact]
    public void TheKinds_CoverEveryNodeTheFormatDefines()
    {
        // Guards the guard: a list that stopped naming kinds would leave the check above agreeing
        // about nothing.
        UnionCoverageAssert.AssertCoversEveryMember<Node>(OneOfEveryNodeKind());
    }
}

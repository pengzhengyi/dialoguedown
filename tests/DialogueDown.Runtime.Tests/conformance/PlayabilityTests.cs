using System.Collections.Immutable;
using DialogueDown.Conformance;
using DialogueDown.Playbook.Nodes;
using DialogueDown.TestSupport;
using static DialogueDown.Runtime.Tests.Conformance.PlayabilityAssert;
using static DialogueDown.Runtime.Tests.Conformance.SessionEntries;

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
        AssertNotPlayable(Sent("""{ "frobnicate": 0 }"""));
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
        // A key no matcher owns would end the run where it is read, so the screen reports it before
        // the run starts.
        AssertNotPlayable(Expected("""{ "invalidated": {} }"""));
    }

    [Fact]
    public void WhyNotPlayable_OfASendNoReaderOwns_NamesItsCommand()
    {
        AssertNotPlayable(Sent("""{ "frobnicate": 0 }"""), "nothing sends frobnicate yet");
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
            Expected("""{ "invalidated": {}, "frobnicate": true }"""));

        AssertNotPlayable(session, "nothing checks invalidated yet", "nothing checks frobnicate yet");
    }

    [Fact]
    public void WhyNotPlayable_OfASession_NamesEachUnreadSendOnce()
    {
        var session = ImmutableArray.Create<SessionEntry>(
            SentCommand("next"),
            SentCommand("frobnicate"),
            SentCommand("frobnicate"),
            Sent("""{ "twiddle": 0 }"""),
            Sent("""{ "twiddle": 1 }"""));

        AssertNotPlayable(session, "nothing sends frobnicate yet", "nothing sends twiddle yet");
    }

    [Fact]
    public void WhatTheHarnessDeclines_IsWhatTheRunnerRefuses()
    {
        // The harness screens a playbook before stepping, and the screen restates what the runner
        // can play, so the two must agree: on every kind, and on each way a menu or a random choice
        // asks the world.
        foreach (var (name, node) in EveryKindAndEachNodeThatAsksTheWorld())
        {
            var refused = node.RefusalOnArrival() is not null;

            Assert.True(
                Playability.CanPlay(node) == !refused,
                $"{name}: the harness calls it "
                    + $"{(Playability.CanPlay(node) ? "playable" : "unplayable")}, "
                    + $"but arriving at it {(refused ? "refuses" : "does not refuse")}.");
        }
    }

    [Fact]
    public void TheKinds_CoverEveryNodeTheFormatDefines()
    {
        // Guards the guard: a list that stopped naming kinds would leave the check above agreeing
        // about nothing.
        UnionCoverageAssert.AssertCoversEveryMember<Node>(PlayContextFactory.OneOfEveryNodeKind());
    }

    /// <summary>
    /// One node of every kind, named by its kind, and each menu and random choice that asks the
    /// world, named by the scenario that builds it.
    /// </summary>
    /// <returns>Each node, with the name a failure reports it by.</returns>
    private static IEnumerable<(string Name, Node Node)> EveryKindAndEachNodeThatAsksTheWorld() =>
    [
        .. PlayContextFactory.OneOfEveryNodeKind().Select(node => (node.GetType().Name, node)),
        (
            nameof(PlayContextFactory.AMenuWithAConditionalOption),
            PlayContextFactory.AMenuWithAConditionalOption()),
        (nameof(PlayContextFactory.AMenuWithAQueryInALabel), PlayContextFactory.AMenuWithAQueryInALabel()),
        (
            nameof(PlayContextFactory.ARandomChoiceWithAGuardedRandomOption),
            PlayContextFactory.ARandomChoiceWithAGuardedRandomOption()),
        (nameof(PlayContextFactory.ARandomChoiceWithAKeyWeight), PlayContextFactory.ARandomChoiceWithAKeyWeight()),
    ];
}

using System.Collections.Immutable;
using DialogueDown.Conformance;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Stepping;
using static DialogueDown.Runtime.Tests.Conformance.SessionEntries;
using static DialogueDown.Runtime.Tests.PlaybookNodes;

namespace DialogueDown.Runtime.Tests.Conformance;

public sealed class PlayabilityTests
{
    [Fact]
    public void CanPlay_ASendSomeReaderOwns_Is()
    {
        Assert.True(Playability.CanPlay(SentCommand("next")));
    }

    [Fact]
    public void CanPlay_ASendNoReaderOwns_IsNot()
    {
        Assert.False(Playability.CanPlay(Sent("""{ "choose": 0 }""")));
    }

    [Fact]
    public void CanPlay_AnExpectation_Is()
    {
        // Its claims are checked where they are read, so the entry itself is always takeable.
        Assert.True(Playability.CanPlay(Expected("""{ "ended": true }""")));
    }

    [Fact]
    public void WhyNotPlayable_OfAPlaybook_NamesEachKindOnce()
    {
        var context = PlayContextFactory.Of(
            [Choice(0, leadsTo: 3), Choice(1, leadsTo: 3), Branch(2, leadsTo: 3), End(3)],
            ["Alice"]);

        Assert.Equal(
            new[] { "nothing plays a ChoiceNode yet", "nothing plays a BranchNode yet" },
            Playability.WhyNotPlayable(context));
    }

    [Fact]
    public void WhyNotPlayable_OfASendNoReaderOwns_NamesItsMessage()
    {
        Assert.Equal(
            new[] { """nothing sends {"choose":0} yet""" },
            Playability.WhyNotPlayable(Sent("""{ "choose": 0 }""")));
    }

    [Fact]
    public void WhyNotPlayable_OfWhatCanBeTaken_IsEmpty()
    {
        Assert.Empty(Playability.WhyNotPlayable(SentCommand("next")));
        Assert.Empty(Playability.WhyNotPlayable(Expected("""{ "ended": true }""")));
    }

    [Fact]
    public void WhyNotPlayable_OfASession_NamesEachUnreadSendOnce()
    {
        var session = ImmutableArray.Create<SessionEntry>(
            SentCommand("next"),
            SentCommand("frobnicate"),
            SentCommand("frobnicate"),
            Sent("""{ "choose": 0 }"""));

        Assert.Equal(
            new[] { """nothing sends "frobnicate" yet""", """nothing sends {"choose":0} yet""" },
            Playability.WhyNotPlayable(session));
    }

    [Fact]
    public void WhatTheHarnessDeclines_IsWhatTheRunnerRefuses()
    {
        // The harness screens a playbook before stepping, so a construct nobody has taught reads
        // as untaught rather than as a divergence. That screen states what this build can play a
        // second time, and the two must agree: a case would otherwise be reported as untaught
        // while it plays, or as a divergence when nobody had taught it.
        foreach (var node in OneOfEachKind())
        {
            var context = PlayContextFactory.Of([node, End(1)], ["Alice"]);
            var refused = Arrival.At(context, 0).Events.OfType<Refused>().Any();

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
        var named = OneOfEachKind().Select(node => node.GetType()).ToHashSet();
        var defined = typeof(Node).Assembly.GetTypes()
            .Where(type => type.IsAssignableTo(typeof(Node)) && !type.IsAbstract);

        Assert.Empty(defined.Where(type => !named.Contains(type)).Select(type => type.Name));
    }
}

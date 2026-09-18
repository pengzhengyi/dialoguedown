using DialogueDown.Playbook.Nodes;
using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Stepping;
using static DialogueDown.Runtime.Tests.Conformance.SessionEntries;
using static DialogueDown.Runtime.Tests.PlaybookNodes;

namespace DialogueDown.Runtime.Tests.Conformance;

public sealed class PlayableRunTests
{
    [Fact]
    public void IsPlayable_ASendSomeReaderOwns_Is()
    {
        Assert.True(PlayableRun.IsPlayable(SentCommand("next")));
    }

    [Fact]
    public void IsPlayable_ASendNoReaderOwns_IsNot()
    {
        Assert.False(PlayableRun.IsPlayable(Sent("""{ "choose": 0 }""")));
    }

    [Fact]
    public void IsPlayable_AnExpectation_Is()
    {
        // Its claims are checked where they are read, so the entry itself is always takeable.
        Assert.True(PlayableRun.IsPlayable(Expected("""{ "ended": true }""")));
    }

    [Fact]
    public void ReasonsNotYetPlayable_NamesEverythingTheBuildHasNotLearned()
    {
        // Two kinds the runner cannot play and a send no reader owns: one run names all three,
        // where stopping at the first would take three runs to learn the same thing.
        var context = PlayContextFactory.Of(
            [Choice(0, leadsTo: 2), Branch(1, leadsTo: 2), End(2)],
            ["Alice"]);

        var reasons = PlayableRun.ReasonsNotYetPlayable(context, [Sent("""{ "choose": 0 }""")]);

        Assert.Equal(
            new[]
            {
                "nothing plays a ChoiceNode yet",
                "nothing plays a BranchNode yet",
                """nothing sends {"choose":0} yet""",
            },
            reasons);
    }

    [Fact]
    public void ReasonsNotYetPlayable_NamesAKindOnce()
    {
        var context = PlayContextFactory.Of(
            [Choice(0, leadsTo: 2), Choice(1, leadsTo: 2), End(2)],
            ["Alice"]);

        Assert.Equal(new[] { "nothing plays a ChoiceNode yet" }, PlayableRun.ReasonsNotYetPlayable(context, []));
    }

    [Fact]
    public void ReasonsNotYetPlayable_OfWhatTheBuildCanPlay_IsEmpty()
    {
        var context = PlayContextFactory.Of([End(0)], ["Alice"]);

        Assert.Empty(PlayableRun.ReasonsNotYetPlayable(context, [SentCommand("next")]));
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
                PlayableRun.IsPlayable(node) == !refused,
                $"{node.GetType().Name}: the harness calls it "
                    + $"{(PlayableRun.IsPlayable(node) ? "playable" : "untaught")}, "
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

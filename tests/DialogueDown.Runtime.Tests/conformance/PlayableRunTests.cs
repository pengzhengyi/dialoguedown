using DialogueDown.Playbook.Nodes;
using DialogueDown.TestSupport;

namespace DialogueDown.Runtime.Tests.Conformance;

public sealed class PlayableRunTests
{
    [Fact]
    public void WhatTheHarnessDeclines_IsWhatTheRunnerRefuses()
    {
        // The harness screens a playbook before stepping, so a construct nobody has taught reads
        // as untaught rather than as a divergence. That screen states what this build can play a
        // second time, and the two must agree: a case would otherwise be reported as untaught
        // while it plays, or as a divergence when nobody had taught it.
        foreach (var node in Playbooks.OneOfEveryNodeKind())
        {
            var refused = node.RefusalOnArrival() is not null;

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
        var named = Playbooks.OneOfEveryNodeKind().Select(node => node.GetType()).ToHashSet();
        var defined = UnionMembers.Of<Node>();

        Assert.Empty(defined.Where(type => !named.Contains(type)).Select(type => type.Name));
    }
}

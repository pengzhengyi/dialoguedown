using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Speech;
using DialogueDown.Playbook.Weights;
using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Stepping;

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
        foreach (var node in EveryKind())
        {
            var context = Playbooks.Of([node, new EndNode(1)], ["Alice"]);
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
        var named = EveryKind().Select(node => node.GetType()).ToHashSet();
        var defined = typeof(Node).Assembly.GetTypes()
            .Where(type => type.IsAssignableTo(typeof(Node)) && !type.IsAbstract);

        Assert.Empty(defined.Where(type => !named.Contains(type)).Select(type => type.Name));
    }

    /// <summary>One node of every kind the playbook format defines, each the simplest of its kind.</summary>
    private static IEnumerable<Node> EveryKind() =>
    [
        new LineNode(0, 0, [new TextFragment("Hello.")], Condition: null, [new SuccessionEdge(1)]),
        new EndNode(0),
        new ControlNode(0, [], Condition: null, [new SuccessionEdge(1)]),
        new ControlNode(0, [new DefaultCommandFragment("fade in")], Condition: null, [new SuccessionEdge(1)]),
        new ChoiceNode(0, Ordered: false, [new OptionEdge(1, [new TextFragment("Go east")], Condition: null)]),
        new BranchNode(0, [new BranchEdge(1, Order: 0, Condition: null)]),
        new RandomChoiceNode(0, [new RandomOptionEdge(1, new AutoWeight(), Condition: null)]),
    ];
}

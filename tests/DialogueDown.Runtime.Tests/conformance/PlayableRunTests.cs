using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Playbook.Speech;
using DialogueDown.Playbook.Weights;
using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Stepping;
using static DialogueDown.Runtime.Tests.Conformance.SessionEntries;

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
    public void ReasonsNotYetRunnable_NamesEverythingTheBuildHasNotLearned()
    {
        // Two kinds the runner cannot play and a send no reader owns: one run names all three,
        // where stopping at the first would take three runs to learn the same thing.
        var context = Playbooks.Of(
            [
                new ChoiceNode(0, Ordered: false, [new OptionEdge(2, [new TextFragment("Go east")], Condition: null)]),
                new BranchNode(1, [new BranchEdge(2, Order: 0, Condition: null)]),
                new EndNode(2),
            ],
            ["Alice"]);

        var reasons = PlayableRun.ReasonsNotYetRunnable(context, [Sent("""{ "choose": 0 }""")]);

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
    public void ReasonsNotYetRunnable_NamesAKindOnce()
    {
        var context = Playbooks.Of(
            [
                new ChoiceNode(0, Ordered: false, [new OptionEdge(2, [new TextFragment("Go east")], Condition: null)]),
                new ChoiceNode(1, Ordered: false, [new OptionEdge(2, [new TextFragment("Go west")], Condition: null)]),
                new EndNode(2),
            ],
            ["Alice"]);

        Assert.Equal(new[] { "nothing plays a ChoiceNode yet" }, PlayableRun.ReasonsNotYetRunnable(context, []));
    }

    [Fact]
    public void ReasonsNotYetRunnable_OfWhatTheBuildCanPlay_IsEmpty()
    {
        var context = Playbooks.Of([new EndNode(0)], ["Alice"]);

        Assert.Empty(PlayableRun.ReasonsNotYetRunnable(context, [SentCommand("next")]));
    }
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

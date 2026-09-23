using System.Collections.Immutable;
using DialogueDown.Playbook.Conditions;
using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Runtime.Protocol;
using DialogueDown.Runtime.Stepping;
using DialogueDown.TestSupport;

namespace DialogueDown.Runtime.Tests.Stepping;

public sealed class ConditionEvaluationExtensionsTests
{
    /// <summary>One condition of every kind the playbook format defines.</summary>
    public static ImmutableArray<Condition> OneOfEveryConditionKind() =>
        [new KeyCondition("Hero.HasSword")];

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Holds_OfAKeyCondition_IsWhatTheWorldSaidAboutThatKey(bool said) =>
        Assert.Equal(
            said, new KeyCondition("Hero.HasSword").Holds(Answering("Hero.HasSword", said)));

    [Fact]
    public void Holds_IsReadForEveryConditionKind()
    {
        var samples = OneOfEveryConditionKind();

        // Checked first, because the walk below is only as complete as the list it walks: a kind
        // added to the format and left out here would go unread with nothing to say so.
        UnionCoverageAssert.AssertCoversEveryMember<Condition>(samples);

        // What each kind reads to depends on the kind; that every kind can be read does not.
        // This is the only guard on the refusing arm, since Condition is closed at the Playbook
        // assembly and no kind outside it can be built to reach that arm directly.
        Assert.All(
            samples,
            condition => Assert.Null(Record.Exception(() => condition.Holds(AnsweringEvery(condition)))));
    }

    [Fact]
    public void Holds_IsNotReadFromNothing() =>
        Assert.Throws<ArgumentNullException>(
            () => ((Condition)null!).Holds(Answering("Hero.HasSword", true)));

    [Fact]
    public void Holds_IsNotReadWithoutWhatTheWorldSaid() =>
        Assert.Throws<ArgumentNullException>(
            () => new KeyCondition("Hero.HasSword").Holds(null!));

    [Fact]
    public void IsAllowed_WhatNobodyGuarded_IsAlwaysSo() =>
        // A jump a writer left unguarded, so asking whether it is allowed does not mean first
        // asking whether anybody guarded it.
        Assert.True(
            new DivertEdge(1, [], Condition: null).IsAllowed(Answering("Hero.HasSword", false)));

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsAllowed_AGuardedWayOut_IsWhatItsGuardReadsTo(bool said) =>
        Assert.Equal(
            said,
            AGuarded(new DivertEdge(1, [], new KeyCondition("Hero.HasSword")), said));

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsAllowed_AGuardedNode_IsWhatItsGuardReadsTo(bool said) =>
        // A node and an edge are read the same way, which is what lets one reader serve both.
        Assert.Equal(
            said,
            AGuarded(
                new LineNode(0, 0, [], new KeyCondition("Hero.HasSword"), [new SuccessionEdge(1)]),
                said));

    [Fact]
    public void IsAllowed_IsNotReadFromNothing() =>
        Assert.Throws<ArgumentNullException>(
            () => ((IConditional)null!).IsAllowed(Answering("Hero.HasSword", true)));

    [Fact]
    public void IsAllowed_IsNotReadWithoutWhatTheWorldSaid() =>
        // Refused even for something nobody guarded, so the contract does not depend on the answer.
        Assert.Throws<ArgumentNullException>(
            () => new DivertEdge(1, [], Condition: null).IsAllowed(null!));

    private static bool AGuarded(IConditional guarded, bool said) =>
        guarded.IsAllowed(Answering("Hero.HasSword", said));

    private static Supply Answering(string key, bool said) =>
        new(ImmutableDictionary.Create<string, Answer>(StringComparer.Ordinal)
            .Add(key, new BooleanAnswer(said)));

    private static Supply AnsweringEvery(Condition condition) =>
        new(condition.Keys().ToImmutableDictionary(
            key => key, Answer (_) => new BooleanAnswer(true), StringComparer.Ordinal));
}

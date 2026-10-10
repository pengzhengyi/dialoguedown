using System.Collections.Immutable;
using DialogueDown.Playbook.Conditions;
using DialogueDown.TestSupport;

namespace DialogueDown.Playbook.Tests.Conditions;

public sealed class ConditionExtensionsTests
{
    /// <summary>One condition of every kind the playbook format defines.</summary>
    public static ImmutableArray<Condition> OneOfEveryConditionKind() => [new KeyCondition("Hero.HasSword")];

    [Fact]
    public void Keys_OfAKeyCondition_NameTheKeyItAsksAbout() =>
        Assert.Equal(["Hero.HasSword"], new KeyCondition("Hero.HasSword").Keys());

    [Fact]
    public void Keys_AreReadFromEveryConditionKind()
    {
        var samples = OneOfEveryConditionKind();

        // Checked first, because the walk below is only as complete as the list it walks: a kind
        // added to the format and left out here would go unread with nothing to say so.
        UnionCoverageAssert.AssertCoversEveryMember<Condition>(samples);

        foreach (var condition in samples)
        {
            Assert.NotEmpty(condition.Keys());
        }
    }

    [Fact]
    public void Keys_RefuseAKindTheyWereNeverTaught() =>
        Assert.Throws<NotSupportedException>(() => new UnhandledCondition().Keys());

    [Fact]
    public void Keys_RejectNothing() =>
        Assert.Throws<ArgumentNullException>(() => ((Condition)null!).Keys());

    /// <summary>A condition kind no reader handles, for the refusal case.</summary>
    private sealed record UnhandledCondition : Condition;
}

using DialogueDown.Playbook.Conditions;
using DialogueDown.Playbook.Edges;
using DialogueDown.Playbook.Nodes;
using DialogueDown.Runtime.Stepping;

namespace DialogueDown.Runtime.Tests.Stepping;

public sealed class NodeQuestionExtensionsTests
{
    [Fact]
    public void KeysToPlay_OfAnUnguardedLine_AreNone() =>
        Assert.Empty(Line(condition: null).KeysToPlay());

    [Fact]
    public void KeysToPlay_OfAGuardedLine_NameWhatItsOwnConditionAsks() =>
        Assert.Equal(["Hero.HasSword"], Line(new KeyCondition("Hero.HasSword")).KeysToPlay());

    [Fact]
    public void KeysToPlay_OfAKindThatCarriesNoCondition_AreNone() =>
        // An end is never guarded, so nothing has to be answered before arriving at one.
        Assert.Empty(new EndNode(0).KeysToPlay());

    [Fact]
    public void KeysToLeave_WhenTheOnlyWayOutIsTheFallThrough_AreNone() =>
        Assert.Empty(Line(condition: null, new SuccessionEdge(1)).KeysToLeave());

    [Fact]
    public void KeysToLeave_NameWhatEveryGuardedWayOutAsks() =>
        Assert.Equal(
            ["Hero.HasSword", "Hero.HasShield"],
            Line(
                condition: null,
                Option(1, "Hero.HasSword"),
                Option(2, "Hero.HasShield"),
                new SuccessionEdge(3))
                .KeysToLeave());

    [Fact]
    public void KeysToLeave_NameARepeatedKeyOnce() =>
        // One reading of the world decides the whole menu, so it cannot offer one option and
        // withhold another on the same key.
        Assert.Equal(
            ["Hero.HasSword"],
            Line(condition: null, Option(1, "Hero.HasSword"), Option(2, "Hero.HasSword"))
                .KeysToLeave());

    [Fact]
    public void KeysToPlay_AreNotReadFromNothing() =>
        Assert.Throws<ArgumentNullException>(() => ((Node)null!).KeysToPlay());

    [Fact]
    public void KeysToLeave_AreNotReadFromNothing() =>
        Assert.Throws<ArgumentNullException>(() => ((Node)null!).KeysToLeave());

    private static LineNode Line(Condition? condition, params Edge[] ways) =>
        new(0, 0, [], condition, [.. ways]);

    private static OptionEdge Option(int target, string key) =>
        new(target, [], new KeyCondition(key));
}

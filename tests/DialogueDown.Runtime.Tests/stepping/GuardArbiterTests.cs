using DialogueDown.Runtime.Stepping;
using static DialogueDown.Runtime.Tests.World;
using static DialogueDown.TestSupport.PlaybookNodeFactory;

namespace DialogueDown.Runtime.Tests.Stepping;

public sealed class GuardArbiterTests
{
    [Fact]
    public void Excludes_ARandomOptionNobodyGuarded_Never_EvenWhenTheWorldWasAskedNothing() =>
        Assert.False(new GuardArbiter(supply: null).Excludes(Chance(50)));

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Excludes_AGuardedRandomOption_ExactlyWhenTheWorldWithholdsItsGuard(bool holds)
    {
        // - `Guard.IsAngry?` `50%` Guard: Halt!
        var arbiter = new GuardArbiter(Answering(("Guard.IsAngry", holds)));

        Assert.Equal(!holds, arbiter.Excludes(Chance(50, condition: "Guard.IsAngry")));
    }

    [Fact]
    public void Excludes_AGuardedRandomOption_CannotBeJudged_WhenTheWorldWasAskedNothing() =>
        Assert.Throws<InvalidOperationException>(
            () => new GuardArbiter(supply: null).Excludes(Chance(50, condition: "Guard.IsAngry")));

    [Fact]
    public void Excludes_RejectsAMissingRandomOption() =>
        Assert.Throws<ArgumentNullException>(() => new GuardArbiter(Answering()).Excludes(null!));
}

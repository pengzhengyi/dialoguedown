using DialogueDown.Runtime.Protocol;

namespace DialogueDown.Runtime.Tests.Conformance.Matchers;

public sealed class RefusalReasonExtensionsTests
{
    public static TheoryData<RefusalReason> Reasons() => [.. Enum.GetValues<RefusalReason>()];

    [Theory]
    [MemberData(nameof(Reasons))]
    public void Name_EveryReason_IsWritten(RefusalReason reason)
    {
        Assert.NotEmpty(reason.Name());
    }

    [Theory]
    [MemberData(nameof(Reasons))]
    public void Matches_TheNameItsOwnReasonIsGiven_IsTrue(RefusalReason reason)
    {
        Assert.True(reason.Matches(reason.Name()));
    }

    [Fact]
    public void Matches_AnotherReasonsName_IsFalse()
    {
        Assert.False(RefusalReason.NotStarted.Matches(RefusalReason.AlreadyEnded.Name()));
    }

    [Fact]
    public void Name_NoTwoReasonsShareOne()
    {
        var names = Enum.GetValues<RefusalReason>().Select(reason => reason.Name()).ToList();

        Assert.Equal(names.Count, names.Distinct(StringComparer.Ordinal).Count());
    }
}

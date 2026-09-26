using DialogueDown.Runtime.Situations;

namespace DialogueDown.Runtime.Tests.Situations;

public sealed class MomentExtensionsTests
{
    [Fact]
    public void Describe_ReadsAsPartOfASentence()
    {
        Assert.Equal("before it plays", Moment.ToPlay.Describe());
        Assert.Equal("before it leaves", Moment.ToLeave.Describe());
    }

    [Fact]
    public void Describe_EveryMoment_IsWorded() =>
        // A moment added later arrives here as a failure rather than as a run that says where it
        // stands with a blank where the moment should be.
        Assert.All(
            Enum.GetValues<Moment>(),
            moment => Assert.False(string.IsNullOrWhiteSpace(moment.Describe())));

    [Fact]
    public void Describe_TellsEveryMomentApartFromTheRest()
    {
        var moments = Enum.GetValues<Moment>();

        Assert.Equal(moments.Length, moments.Select(moment => moment.Describe()).Distinct().Count());
    }

    [Fact]
    public void Describe_RefusesAMomentItWasNeverTaught() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => ((Moment)999).Describe());
}

using DialogueDown.Runtime.Stepping;
using static DialogueDown.Runtime.Tests.World;

namespace DialogueDown.Runtime.Tests.Stepping;

public sealed class AnsweredKeyWeightsTests
{
    [Fact]
    public void NumberFor_AKeyWeight_IsTheNumberTheWorldSuppliedForItsKey()
    {
        // - `Guard.Mood%` Guard: Lovely day.
        var keyWeights = new AnsweredKeyWeights(Answering(("Guard.Mood", 20.0)));

        Assert.Equal(20.0, keyWeights.NumberFor("Guard.Mood"));
    }

    [Fact]
    public void NumberFor_AKeyWeight_CannotBeGivenANumber_WhenTheWorldWasAskedNothing() =>
        Assert.Throws<InvalidOperationException>(() => new AnsweredKeyWeights(supply: null).NumberFor("Guard.Mood"));

    [Fact]
    public void NumberFor_RejectsAMissingKey() =>
        Assert.Throws<ArgumentNullException>(() => new AnsweredKeyWeights(Answering()).NumberFor(null!));
}

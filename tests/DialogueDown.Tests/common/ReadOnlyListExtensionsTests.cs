using DialogueDown.Common;

namespace DialogueDown.Tests.Common;

public sealed class ReadOnlyListExtensionsTests
{
    [Fact]
    public void FindIndex_ReturnsTheFirstMatch()
    {
        IReadOnlyList<int> values = [10, 20, 30, 20];

        Assert.Equal(1, values.FindIndex(value => value == 20));
    }

    [Fact]
    public void FindIndex_NoMatch_ReturnsMinusOne()
    {
        IReadOnlyList<int> values = [1, 2, 3];

        Assert.Equal(-1, values.FindIndex(value => value == 99));
    }

    [Fact]
    public void FindIndex_EmptyList_ReturnsMinusOne()
    {
        Assert.Equal(-1, Array.Empty<int>().FindIndex(_ => true));
    }

    [Fact]
    public void FindIndex_NullSource_Throws() =>
        Assert.Throws<ArgumentNullException>(
            () => ((IReadOnlyList<int>)null!).FindIndex(_ => true));

    [Fact]
    public void FindIndex_NullPredicate_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new[] { 1 }.FindIndex(null!));
}

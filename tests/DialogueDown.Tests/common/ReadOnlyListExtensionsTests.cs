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

    [Fact]
    public void ReplaceOrRemoveAt_NonNullReplacement_SwapsThatElement()
    {
        IReadOnlyList<string> values = ["a", "b", "c"];

        Assert.Equal(["a", "B", "c"], values.ReplaceOrRemoveAt(1, "B"));
    }

    [Fact]
    public void ReplaceOrRemoveAt_NullReplacement_RemovesThatElement()
    {
        IReadOnlyList<string> values = ["a", "b", "c"];

        Assert.Equal(["a", "c"], values.ReplaceOrRemoveAt(1, null));
    }

    [Fact]
    public void ReplaceOrRemoveAt_AtTheFirstIndex_AffectsOnlyThatElement()
    {
        IReadOnlyList<string> values = ["a", "b"];

        Assert.Equal(["b"], values.ReplaceOrRemoveAt(0, null));
        Assert.Equal(["A", "b"], values.ReplaceOrRemoveAt(0, "A"));
    }

    [Fact]
    public void ReplaceOrRemoveAt_DoesNotModifyTheSource()
    {
        IReadOnlyList<string> values = ["a", "b"];

        values.ReplaceOrRemoveAt(0, null);

        Assert.Equal(["a", "b"], values);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    public void ReplaceOrRemoveAt_IndexOutsideTheList_Throws(int index) =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new[] { "a", "b" }.ReplaceOrRemoveAt(index, "x"));

    [Fact]
    public void ReplaceOrRemoveAt_NullSource_Throws() =>
        Assert.Throws<ArgumentNullException>(
            () => ((IReadOnlyList<string>)null!).ReplaceOrRemoveAt(0, "x"));
}

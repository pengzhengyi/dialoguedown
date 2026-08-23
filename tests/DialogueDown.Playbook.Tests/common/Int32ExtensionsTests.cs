using DialogueDown.Playbook.Common;
namespace DialogueDown.Playbook.Tests.Common;

public sealed class Int32ExtensionsTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    public void AssertNotNegative_APosition_ReturnsIt(int value)
    {
        // Zero is the boundary that matters: the first node and the first branch arm both sit
        // there, so a guard that rejected it would refuse every playbook.
        Assert.Equal(value, value.AssertNotNegative("target"));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void AssertNotNegative_NotAPosition_NamesTheParameter(int value)
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => value.AssertNotNegative("target"));

        Assert.Equal("target", error.ParamName);
    }
}

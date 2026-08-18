namespace DialogueDown.Playbook.Tests;

public sealed class StringExtensionsTests
{
    [Fact]
    public void AssertNotNull_PresentString_ReturnsIt()
    {
        Assert.Equal("portrait.png", "portrait.png".AssertNotNull("source"));
    }

    [Fact]
    public void AssertNotNull_EmptyString_ReturnsIt()
    {
        // Empty is a value; only absence is a programming error. A link target may legitimately
        // be empty, which is why this guard is separate from AssertNotEmpty.
        Assert.Equal(string.Empty, string.Empty.AssertNotNull("target"));
    }

    [Fact]
    public void AssertNotNull_MissingString_NamesTheParameter()
    {
        var error = Assert.Throws<ArgumentNullException>(() => ((string?)null).AssertNotNull("source"));

        Assert.Equal("source", error.ParamName);
    }

    [Fact]
    public void AssertNotEmpty_PresentString_ReturnsIt()
    {
        Assert.Equal("hello", "hello".AssertNotEmpty("text"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void AssertNotEmpty_NothingToSay_NamesTheParameter(string? value)
    {
        var error = Assert.ThrowsAny<ArgumentException>(() => value.AssertNotEmpty("text"));

        Assert.Equal("text", error.ParamName);
    }
}

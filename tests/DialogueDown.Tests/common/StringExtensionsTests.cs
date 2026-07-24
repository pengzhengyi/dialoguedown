using DialogueDown.Common;

namespace DialogueDown.Tests.Common;

public sealed class StringExtensionsTests
{
    [Fact]
    public void NullIfEmpty_NonEmpty_IsUnchanged() => Assert.Equal("hi", "hi".NullIfEmpty());

    [Fact]
    public void NullIfEmpty_Empty_IsNull() => Assert.Null(string.Empty.NullIfEmpty());

    [Fact]
    public void NullIfEmpty_Null_IsNull() => Assert.Null(((string?)null).NullIfEmpty());

    [Theory]
    [InlineData(" hi")]
    [InlineData("\thi")]
    [InlineData("\n")]
    public void HasLeadingWhitespace_IsTrue_WhenItBeginsWithWhitespace(string value) =>
        Assert.True(value.HasLeadingWhitespace());

    [Theory]
    [InlineData("hi")]
    [InlineData("hi ")]
    [InlineData("")]
    public void HasLeadingWhitespace_IsFalse_Otherwise(string value) =>
        Assert.False(value.HasLeadingWhitespace());
}

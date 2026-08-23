using System.Collections.Immutable;
using DialogueDown.Playbook.Common;
namespace DialogueDown.Playbook.Tests.Common;

public sealed class ImmutableArrayExtensionsTests
{
    [Fact]
    public void OrEmpty_UninitializedArray_BecomesEmpty()
    {
        Assert.Empty(default(ImmutableArray<string>).OrEmpty());
    }

    [Fact]
    public void OrEmpty_PopulatedArray_IsUnchanged()
    {
        var capabilities = ImmutableArray.Create("core");

        Assert.Equal(capabilities, capabilities.OrEmpty());
    }

    [Fact]
    public void AssertNotEmpty_PopulatedArray_ReturnsIt()
    {
        var fragments = ImmutableArray.Create("word");

        Assert.Equal(fragments, fragments.AssertNotEmpty("children"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AssertNotEmpty_NothingToWrap_NamesTheParameterAndTheElementType(bool uninitialized)
    {
        // Uninitialized and empty are the same failure to a caller that needs content.
        var nothing = uninitialized ? default : ImmutableArray<string>.Empty;

        var error = Assert.Throws<ArgumentException>(() => nothing.AssertNotEmpty("children"));

        Assert.Equal("children", error.ParamName);
        Assert.Contains("String", error.Message, StringComparison.Ordinal);
    }
}

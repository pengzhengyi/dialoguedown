using System.Collections.Immutable;
using DialogueDown.Common;

namespace DialogueDown.Tests.Common;

public sealed class ImmutableArrayExtensionsTests
{
    [Fact]
    public void AssertInitialized_PopulatedArray_ReturnsIt()
    {
        var speakers = ImmutableArray.Create("Alice", "Bob");

        Assert.Equal(speakers, speakers.AssertInitialized("speakers"));
    }

    [Fact]
    public void AssertInitialized_EmptyArray_ReturnsIt()
    {
        // Empty is initialized: the distinction that matters is "no elements" versus
        // "no array at all", and only the second is a programming error.
        var empty = ImmutableArray<string>.Empty;

        Assert.Equal(empty, empty.AssertInitialized("speakers"));
    }

    [Fact]
    public void AssertInitialized_DefaultArray_NamesTheParameterAndTheElementType()
    {
        void Uninitialized() => _ = default(ImmutableArray<string>).AssertInitialized("speakers");

        var error = Assert.Throws<ArgumentException>(Uninitialized);
        Assert.Equal("speakers", error.ParamName);
        Assert.Contains("String", error.Message, StringComparison.Ordinal);
    }
}

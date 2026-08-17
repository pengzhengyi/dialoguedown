using System.Collections.Immutable;

namespace DialogueDown.Playbook.Tests;

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
}

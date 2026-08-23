using DialogueDown.Playbook.Common;
namespace DialogueDown.Playbook.Tests.Common;

public sealed class ArrayExtensionsTests
{
    [Fact]
    public void AssertNoneMissing_AFullArray_ReturnsIt()
    {
        string[] values = ["one", "two"];

        Assert.Same(values, values.AssertNoneMissing(nameof(values)));
    }

    [Fact]
    public void AssertNoneMissing_AnEmptyArray_ReturnsIt()
    {
        // Nothing is missing from a list of nothing. Whether empty is meaningful is the
        // caller's question, not this guard's.
        string[] values = [];

        Assert.Same(values, values.AssertNoneMissing(nameof(values)));
    }

    [Fact]
    public void AssertNoneMissing_NoArrayAtAll_IsRejected()
    {
        string[]? values = null;

        var error = Assert.Throws<ArgumentNullException>(() => values.AssertNoneMissing("values"));

        Assert.Equal("values", error.ParamName);
    }

    [Fact]
    public void AssertNoneMissing_AGapInTheArray_NamesTheElementType()
    {
        string[] values = ["one", null!];

        var error = Assert.Throws<ArgumentException>(() => values.AssertNoneMissing("values"));

        Assert.Equal("values", error.ParamName);
        Assert.Contains(nameof(String), error.Message, StringComparison.Ordinal);
    }
}

namespace DialogueDown.Runtime.Tests.Conformance;

public sealed class KeyedHandlersTests
{
    [Fact]
    public void ByKey_DistinctKeys_FilesEachHandlerUnderItsKey()
    {
        var byKey = KeyedHandlers.ByKey(handler => handler, "next", "done");

        Assert.Equal("next", byKey["next"]);
        Assert.Equal("done", byKey["done"]);
    }

    [Fact]
    public void ByKey_TwoHandlersClaimingOneKey_Throws()
    {
        Assert.Throws<ArgumentException>(() => KeyedHandlers.ByKey(handler => handler, "next", "next"));
    }

    [Fact]
    public void ByKey_KeysDifferingOnlyInCase_AreDistinct()
    {
        var byKey = KeyedHandlers.ByKey(handler => handler, "next", "Next");

        Assert.Equal(2, byKey.Count);
    }
}

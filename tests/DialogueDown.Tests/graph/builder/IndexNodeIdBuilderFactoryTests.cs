using DialogueDown.Graph.Builder;

namespace DialogueDown.Tests.Graph.Builder;

public sealed class IndexNodeIdBuilderFactoryTests
{
    [Fact]
    public void Create_EachCallReturnsAFreshBuilder()
    {
        var factory = new IndexNodeIdBuilderFactory();

        var first = factory.Create();
        var second = factory.Create();

        Assert.IsType<IndexNodeIdBuilder>(first);
        Assert.IsType<IndexNodeIdBuilder>(second);
        Assert.NotSame(first, second);
    }
}

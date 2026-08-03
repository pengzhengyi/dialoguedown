using DialogueDown.Graph;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.NodeIdAssert;

namespace DialogueDown.Tests.Graph;

public sealed class IndexNodeIdBuilderTests
{
    private readonly IndexNodeIdBuilder _builder = new();

    [Fact]
    public void GetOrAssign_Null_Throws() =>
        Assert.Throws<ArgumentNullException>(() => _builder.GetOrAssign(null!));

    [Fact]
    public void GetOrAssign_AssignsIdsIncrementally_AndCachesEachBlock()
    {
        var blocks = Pipeline.Blocks("Alice: a\n\nBob: b");

        var first = _builder.GetOrAssign(blocks[0]);
        var repeated = _builder.GetOrAssign(blocks[0]);
        var second = _builder.GetOrAssign(blocks[1]);

        AssertIdEqual(0, first);
        Assert.Equal(first, repeated);
        AssertIdEqual(1, second);
        Assert.Equal(first, _builder.Get(blocks[0]));
    }

    [Fact]
    public void GetOrAssignEnd_AssignsOneCachedId()
    {
        var first = _builder.GetOrAssignEnd();
        var repeated = _builder.GetOrAssignEnd();

        AssertIdEqual(0, first);
        Assert.Equal(first, repeated);
    }

    [Fact]
    public void Get_UnassignedBlock_Throws()
    {
        var block = Assert.Single(Pipeline.Blocks("Alice: a"));

        Assert.Throws<ArgumentException>(() => _builder.Get(block));
    }

    [Fact]
    public void Freeze_WithoutEnd_Throws() =>
        Assert.Throws<InvalidOperationException>(() => _builder.Freeze());

    [Fact]
    public void Freeze_ReturnsTheAssignments_AndPreventsFurtherAdditions()
    {
        var block = Assert.Single(Pipeline.Blocks("Alice: a"));
        var id = _builder.GetOrAssign(block);
        var end = _builder.GetOrAssignEnd();

        var map = _builder.Freeze();

        Assert.Equal(id, map[block]);
        Assert.Equal(end, map.End);
        Assert.Same(map, _builder.Freeze());
        Assert.Throws<InvalidOperationException>(() => _builder.GetOrAssign(block));
    }
}

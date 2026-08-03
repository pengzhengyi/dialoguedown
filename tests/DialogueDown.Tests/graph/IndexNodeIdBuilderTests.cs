using DialogueDown.Graph;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.NodeIdAssert;

namespace DialogueDown.Tests.Graph;

public sealed class IndexNodeIdBuilderTests
{
    private readonly IndexNodeIdBuilder _builder = new();

    [Fact]
    public void Assign_Null_Throws() =>
        Assert.Throws<ArgumentNullException>(() => _builder.Assign(null!));

    [Fact]
    public void Assign_GivesEachBlockItsDocumentIndex_AndEndLast()
    {
        var blocks = Pipeline.Blocks("Alice: a\n\nBob: b");

        var ids = _builder.Assign(blocks);

        AssertIdEqual(0, ids.Of(blocks[0]));
        AssertIdEqual(1, ids.Of(blocks[1]));
        AssertIdEqual(2, ids.End);
    }
}

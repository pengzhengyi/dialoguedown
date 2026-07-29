using DialogueDown.Graph;
using DialogueDown.Script.Semantics;
using DialogueDown.Tests.Support;

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
        var blocks = Pipeline.UntilAnalyzed("Alice: a\n\nBob: b").SceneRoot.DocumentOrder();

        var ids = _builder.Assign(blocks);

        Assert.Equal(new NodeId(0), ids.Of(blocks[0]));
        Assert.Equal(new NodeId(1), ids.Of(blocks[1]));
        Assert.Equal(new NodeId(2), ids.End);
    }
}

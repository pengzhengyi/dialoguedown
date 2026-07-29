using DialogueDown.Graph;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Graph;

public sealed class DialogueGraphBuilderTests
{
    private readonly DialogueGraphBuilder _builder = new();

    [Fact]
    public void Build_NullModel_Throws() =>
        Assert.Throws<ArgumentNullException>(
            () => _builder.Build(null!, DiagnosticsContextFactory.Context("")));

    [Fact]
    public void Build_NullContext_Throws() =>
        Assert.Throws<ArgumentNullException>(() => _builder.Build(Pipeline.UntilAnalyzed(""), null!));

    [Fact]
    public void Build_EmptyDocument_EntryIsTheEndNode()
    {
        var graph = Build("");

        Assert.IsType<EndNode>(graph.Node(graph.Entry));
        Assert.Equal(graph.Entry, graph.End);
    }

    private DialogueGraph Build(string source) =>
        _builder.Build(Pipeline.UntilAnalyzed(source), DiagnosticsContextFactory.Context(source));
}

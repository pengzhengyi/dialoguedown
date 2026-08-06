using DialogueDown.Graph.Builder;
using DialogueDown.Graph.Passes;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Graph.Builder;

public sealed class DialogueGraphBuilderTests
{
    private readonly List<IGraphBuildPass> _applied = [];

    [Fact]
    public void Build_NullModel_Throws() =>
        Assert.Throws<ArgumentNullException>(
            () => Builder().Build(null!, DiagnosticsContextFactory.Context("")));

    [Fact]
    public void Build_NullContext_Throws() =>
        Assert.Throws<ArgumentNullException>(
            () => Builder().Build(Pipeline.UntilAnalyzed(""), null!));

    [Fact]
    public void Build_AppliesEachPassInOrderOnOneSharedDraft()
    {
        var terminate = new RecordingPass(_applied, addsEnd: true);
        var follow = new RecordingPass(_applied);
        var builder = new DialogueGraphBuilder(new IndexNodeIdBuilderFactory(), [terminate, follow]);

        builder.Build(Pipeline.UntilAnalyzed(""), DiagnosticsContextFactory.Context(""));

        Assert.Equal([terminate, follow], _applied);
        Assert.Same(terminate.LastDraft, follow.LastDraft);
    }

    [Fact]
    public void Build_CreatesAFreshIdBuilderPerBuild()
    {
        var factory = new CountingIdBuilderFactory();
        var builder = new DialogueGraphBuilder(factory, [new RecordingPass(_applied, addsEnd: true)]);

        builder.Build(Pipeline.UntilAnalyzed(""), DiagnosticsContextFactory.Context(""));
        builder.Build(Pipeline.UntilAnalyzed(""), DiagnosticsContextFactory.Context(""));

        Assert.Equal(2, factory.Creates);
    }

    private DialogueGraphBuilder Builder() =>
        new(new IndexNodeIdBuilderFactory(), [new RecordingPass(_applied, addsEnd: true)]);

    private sealed class RecordingPass(List<IGraphBuildPass> applied, bool addsEnd = false)
        : IGraphBuildPass
    {
        public GraphDraft? LastDraft { get; private set; }

        public void Apply(GraphDraft draft, GraphBuildContext context)
        {
            LastDraft = draft;
            applied.Add(this);
            if (addsEnd)
            {
                draft.AddEnd(SourceSpanFactory.Span());
            }
        }
    }

    private sealed class CountingIdBuilderFactory : INodeIdBuilderFactory
    {
        public int Creates { get; private set; }

        public INodeIdBuilder Create()
        {
            Creates++;
            return new IndexNodeIdBuilder();
        }
    }
}

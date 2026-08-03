using DialogueDown.Graph;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Graph;

public sealed class GraphBuildContextTests
{
    [Fact]
    public void Constructor_NullSemantics_Throws() =>
        Assert.Throws<ArgumentNullException>(
            () => new GraphBuildContext(null!, DiagnosticsContextFactory.Context()));

    [Fact]
    public void Constructor_NullDiagnostics_Throws() =>
        Assert.Throws<ArgumentNullException>(
            () => new GraphBuildContext(Pipeline.UntilAnalyzed(""), null!));

    [Fact]
    public void Constructor_HoldsInputs_AndCachesDocumentOrder()
    {
        const string Source = """
            # A

            Alice: a

            ## B

            Bob: b
            """;
        var semantics = Pipeline.UntilAnalyzed(Source);
        var diagnostics = DiagnosticsContextFactory.Context(Source);

        var context = new GraphBuildContext(semantics, diagnostics);
        var firstRead = context.Blocks;
        var secondRead = context.Blocks;

        Assert.Same(semantics, context.Semantics);
        Assert.Same(diagnostics, context.Diagnostics);
        Assert.Same(firstRead, secondRead);
        Assert.Equal(2, firstRead.Count);
        Assert.True(firstRead[0].Span.Start < firstRead[1].Span.Start);
    }
}

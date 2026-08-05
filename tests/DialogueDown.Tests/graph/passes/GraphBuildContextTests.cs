using DialogueDown.Graph.Passes;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Graph.Passes;

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
        var firstRead = context.TopLevelBlocks;
        var secondRead = context.TopLevelBlocks;

        Assert.Same(semantics, context.Semantics);
        Assert.Same(diagnostics, context.Diagnostics);
        Assert.Same(firstRead, secondRead);
        Assert.Equal(2, firstRead.Count);
        Assert.True(firstRead[0].Span.Start < firstRead[1].Span.Start);
    }

    [Fact]
    public void AllBlocks_AddsTheBlocksNestedInAChoiceOptionsBody()
    {
        const string Source = """
            Guide: Pick.

            - Alice: Left.

            - Alice: Right.
            """;

        var context = new GraphBuildContext(
            Pipeline.UntilAnalyzed(Source), DiagnosticsContextFactory.Context(Source));

        // The question and the choice group are the document's own sequence; the two arms are not.
        Assert.Equal(2, context.TopLevelBlocks.Count);
        Assert.Equal(4, context.AllBlocks.Count);
        Assert.Equal(context.TopLevelBlocks, context.AllBlocks.Take(2));
    }
}

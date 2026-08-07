using DialogueDown.Graph;
using DialogueDown.Graph.Builder;
using DialogueDown.Graph.Edges;
using DialogueDown.Graph.Nodes;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Semantics;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstAssert;
using static DialogueDown.Tests.Support.DialogueGraphFactory;
using static DialogueDown.Tests.Support.GraphDraftFactory;

namespace DialogueDown.Tests.Graph.Builder;

public sealed class GraphDraftTests
{
    [Fact]
    public void Constructor_NullIdBuilder_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new GraphDraft(null!));

    [Fact]
    public void AddEnd_MakesTheEndNodeBothEntryAndEnd()
    {
        var draft = Draft();
        draft.AddEnd(SourceSpanFactory.Span());

        var graph = draft.Freeze();

        Assert.Equal(graph.Entry, graph.End);
        Assert.IsType<EndNode>(graph.Node(graph.End));
    }

    [Fact]
    public void AddBlock_TheFirstTrackedBlockBecomesTheEntry()
    {
        var blocks = Pipeline.Blocks("Alice: a\n\nBob: b", out var model);
        var draft = Draft();
        var firstId = AddLine(draft, model, (Line)blocks[0]);
        AddLine(draft, model, (Line)blocks[1]);
        draft.AddEnd(SourceSpanFactory.Span());

        var graph = draft.Freeze();

        Assert.Equal(firstId, graph.Entry);
        Assert.NotEqual(graph.Entry, graph.End);
    }

    [Fact]
    public void AddBlock_DraftUsesADifferentId_Throws()
    {
        var line = AssertSingleLine(Pipeline.Blocks("Alice: a"));
        var draft = Draft();

        Assert.Throws<InvalidOperationException>(
            () => draft.AddBlock(line, _ => new EndNodeDraft(NodeId(99), SourceSpanFactory.Span())));
    }

    [Fact]
    public void Freeze_WithoutAnEndNode_Throws()
    {
        var line = AssertSingleLine(Pipeline.Blocks("Alice: a", out var model));
        var draft = Draft();
        AddLine(draft, model, line);

        Assert.Throws<InvalidOperationException>(() => draft.Freeze());
    }

    [Fact]
    public void Freeze_EdgeTargetDoesNotIdentifyANode_Throws()
    {
        var line = AssertSingleLine(Pipeline.Blocks("Alice: a", out var model));
        var draft = Draft();
        var lineId = AddLine(draft, model, line);
        draft.AddEnd(SourceSpanFactory.Span());
        draft.AddEdge(lineId, SuccessionEdge(99));

        Assert.Throws<InvalidOperationException>(() => draft.Freeze());
    }

    [Fact]
    public void Freeze_ValidDraft_FreezesEachNodeAndItsEdges()
    {
        var line = AssertSingleLine(Pipeline.Blocks("Alice: a", out var model));
        var draft = Draft();
        var lineId = AddLine(draft, model, line);
        var endId = draft.AddEnd(SourceSpanFactory.Span());
        draft.AddEdge(lineId, SuccessionEdge(endId.Value));

        var graph = draft.Freeze();

        Assert.Equal(lineId, graph.Entry);
        var entry = Assert.IsType<LineNode>(graph.Node(graph.Entry));
        var edge = Assert.IsType<SuccessionEdge>(Assert.Single(entry.Out));
        Assert.Equal(graph.End, edge.Target);
    }

    [Fact]
    public void Freeze_Twice_Throws()
    {
        var draft = Draft();
        draft.AddEnd(SourceSpanFactory.Span());
        draft.Freeze();

        Assert.Throws<InvalidOperationException>(() => draft.Freeze());
    }

    [Fact]
    public void AddEnd_AfterFreeze_Throws()
    {
        var draft = Draft();
        draft.AddEnd(SourceSpanFactory.Span());
        draft.Freeze();

        Assert.Throws<InvalidOperationException>(() => draft.AddEnd(SourceSpanFactory.Span()));
    }

    private static NodeId AddLine(GraphDraft draft, SemanticModel model, Line line) =>
        draft.AddBlock(
            line,
            id => new LineNodeDraft(id, line.Span, model.Speakers.Resolve(line.Speaker!), line.Speech));
}

using DialogueDown.Graph;
using DialogueDown.Graph.Builder;
using DialogueDown.Tests.Support;
using static DialogueDown.Tests.Support.DialogueAstAssert;
using static DialogueDown.Tests.Support.DialogueGraphFactory;
using static DialogueDown.Tests.Support.SpeakerSymbolAssert;

namespace DialogueDown.Tests.Graph.Builder;

public sealed class LineNodeDraftTests
{
    [Fact]
    public void Freeze_CombinesItsPayloadWithAccumulatedEdges()
    {
        var line = AssertSingleLine(Pipeline.Blocks("Alice: a", out var model));
        var draft = new LineNodeDraft(
            NodeId(0),
            AssertHasSpeaker(model, line.Speaker!, "Alice"),
            line.Speech);
        draft.AddSuccessionEdge(1);

        var node = Assert.IsType<LineNode>(draft.Freeze());

        Assert.Equal("Alice", node.Speaker.Name);
        Assert.Same(line.Speech, node.Speech);
        Assert.IsType<Succession>(Assert.Single(node.Out));
        Assert.Throws<InvalidOperationException>(
            () => draft.AddSuccessionEdge(2));
    }
}

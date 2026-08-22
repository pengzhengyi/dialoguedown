using DialogueDown.Common;
using DialogueDown.Graph;
using DialogueDown.Graph.Builder;
using DialogueDown.Graph.Edges;
using DialogueDown.Graph.Nodes;
using DialogueDown.Graph.Regions;
using DialogueDown.Script.Ast;

using DialogueDown.Script.Semantics;
namespace DialogueDown.Tests.Support;

/// <summary>Object Mother and small construction helpers for dialogue-graph tests.</summary>
internal static class DialogueGraphFactory
{
    public static NodeId NodeId(int value) => new(value);

    /// <summary>An End node with the given id, spanning nothing in particular.</summary>
    public static EndNode EndNode(int id) => new(NodeId(id), new SourceSpan(0, 0));

    /// <summary>A line node with the given id, said by <paramref name="speaker"/>.</summary>
    public static LineNode LineNode(
        int id, SpeakerSymbol speaker, params InlineFragment[] speech) =>
        new(NodeId(id), new SourceSpan(0, 0), speaker, speech, []);

    /// <summary>A line node said only when <paramref name="condition"/> holds.</summary>
    public static LineNode ConditionalLineNode(
        int id, SpeakerSymbol speaker, Condition condition, params InlineFragment[] speech) =>
        new(NodeId(id), new SourceSpan(0, 0), speaker, speech, [], condition);

    /// <summary>A choice node offering <paramref name="options"/>.</summary>
    public static ChoiceNode ChoiceNode(int id, bool ordered = false, params Edge[] options) =>
        new(NodeId(id), new SourceSpan(0, 0), ordered, options);

    /// <summary>A random choice node the engine picks from.</summary>
    public static RandomChoiceNode RandomChoiceNode(int id, params Edge[] arms) =>
        new(NodeId(id), new SourceSpan(0, 0), arms);

    /// <summary>A conditional block, whose arms are tried in order.</summary>
    public static BranchNode BranchNode(int id, params Edge[] arms) =>
        new(NodeId(id), new SourceSpan(0, 0), arms);

    /// <summary>A control node running <paramref name="effects"/> and nothing else.</summary>
    public static ControlNode ControlNode(int id, params GameCall[] effects) =>
        new(NodeId(id), new SourceSpan(0, 0), effects, []);

    /// <summary>A scene named <paramref name="anchor"/>, nesting <paramref name="subregions"/>.</summary>
    public static SceneRegion SceneRegion(string anchor, params Region[] subregions) =>
        new(
            new RegionId(0),
            NodeId(0),
            NodeId(0),
            new HashSet<NodeId>(),
            subregions,
            [DialogueAstFactory.Text(anchor)],
            anchor);

    /// <summary>The smallest valid graph: an End node a run starts and finishes on.</summary>
    public static DialogueGraph EmptyGraph()
    {
        var end = NodeId(0);
        return new DialogueGraph(
            [new EndNode(end, new SourceSpan(0, 0))], entry: end, end: end, RegionTree.Empty);
    }

    public static SuccessionEdge SuccessionEdge(int target) => new(NodeId(target));

    public static SuccessionEdge SuccessionEdge(NodeId target) => new(target);

    /// <summary>An option leading to <paramref name="target"/>, labelled as it would be shown.</summary>
    public static OptionEdge OptionEdge(
        NodeId target, string label = "that one", Condition? condition = null) =>
        new(target, [DialogueAstFactory.Text(label)], condition);

    /// <summary>One arm of a random choice, weighted by an even share unless told otherwise.</summary>
    public static RandomOptionEdge RandomOptionEdge(
        NodeId target, ChoiceWeight? weight = null, Condition? condition = null) =>
        new(target, weight ?? DialogueAstFactory.AutoWeight(), condition);

    /// <summary>One arm of a conditional block, tried in <paramref name="order"/>.</summary>
    public static BranchEdge BranchEdge(NodeId target, int order = 0, Condition? condition = null) =>
        new(target, order, condition);

    /// <summary>A divert to <paramref name="target"/>, labelled as a writer would have.</summary>
    public static DivertEdge DivertEdge(
        NodeId target, string label = "there", Condition? condition = null) =>
        new(target, [DialogueAstFactory.Text(label)], condition);

    public static void AddSuccessionEdge(this NodeDraft draft, int target)
    {
        ArgumentNullException.ThrowIfNull(draft);
        draft.AddEdge(SuccessionEdge(target));
    }
}

using DialogueDown.Common;
using DialogueDown.Graph;
using DialogueDown.Graph.Builder;
using DialogueDown.Graph.Edges;
using DialogueDown.Graph.Nodes;
using DialogueDown.Graph.Regions;

namespace DialogueDown.Tests.Support;

/// <summary>Object Mother and small construction helpers for dialogue-graph tests.</summary>
internal static class DialogueGraphFactory
{
    public static NodeId NodeId(int value) => new(value);

    /// <summary>The smallest valid graph: an End node a run starts and finishes on.</summary>
    public static DialogueGraph EmptyGraph()
    {
        var end = NodeId(0);
        return new DialogueGraph(
            [new EndNode(end, new SourceSpan(0, 0))], entry: end, end: end, RegionTree.Empty);
    }

    public static SuccessionEdge SuccessionEdge(int target) => new(NodeId(target));

    public static void AddSuccessionEdge(this NodeDraft draft, int target)
    {
        ArgumentNullException.ThrowIfNull(draft);
        draft.AddEdge(SuccessionEdge(target));
    }
}

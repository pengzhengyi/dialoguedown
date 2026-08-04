using DialogueDown.Graph;
using DialogueDown.Graph.Builder;

namespace DialogueDown.Tests.Support;

/// <summary>Object Mother and small construction helpers for dialogue-graph tests.</summary>
internal static class DialogueGraphFactory
{
    public static NodeId NodeId(int value) => new(value);

    public static Succession Succession(int target) => new(NodeId(target));

    public static void AddSuccessionEdge(this NodeDraft draft, int target)
    {
        ArgumentNullException.ThrowIfNull(draft);
        draft.AddEdge(Succession(target));
    }
}

using DialogueDown.Graph;
using DialogueDown.Graph.Passes;

namespace DialogueDown.Tests.Support;

/// <summary>
/// Builds a dialogue graph by running the given passes, in order, over a fresh draft — so a pass
/// test states just the passes it needs and asserts on the frozen graph.
/// </summary>
internal static class GraphPasses
{
    public static DialogueGraph Build(string source, params IGraphBuildPass[] passes)
    {
        var draft = GraphDraftFactory.Draft();
        var context = GraphBuildContextFactory.Context(source);
        foreach (var pass in passes)
        {
            pass.Apply(draft, context);
        }

        return draft.Freeze();
    }
}

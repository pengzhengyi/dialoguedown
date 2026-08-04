using DialogueDown.Graph.Builder;

namespace DialogueDown.Graph.Passes;

/// <summary>
/// One construction pass over a graph draft. Passes run in dependency order and each owns a
/// single graph concern, such as node creation or succession edges.
/// </summary>
internal interface IGraphBuildPass
{
    /// <summary>Applies this pass to <paramref name="draft"/> using <paramref name="context"/>.</summary>
    void Apply(GraphDraft draft, GraphBuildContext context);
}

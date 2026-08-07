using DialogueDown.Graph.Builder;

namespace DialogueDown.Graph.Passes;

/// <summary>
/// Base class for a graph build pass. It guards the shared inputs so a concrete pass implements
/// only its construction concern in <see cref="ApplyCore"/>.
/// </summary>
internal abstract class GraphBuildPass : IGraphBuildPass
{
    public void Apply(GraphDraft draft, GraphBuildContext context)
    {
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(context);
        ApplyCore(draft, context);
    }

    protected abstract void ApplyCore(GraphDraft draft, GraphBuildContext context);
}

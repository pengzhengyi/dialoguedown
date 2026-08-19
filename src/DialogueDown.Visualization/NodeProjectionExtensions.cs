using DialogueDown.Visualization.Display;
namespace DialogueDown.Visualization;

/// <summary>
/// Ergonomic entry points so a caller can turn an IR into a
/// <see cref="DisplayGraph"/> without naming the walk or the projection type —
/// <c>ir.ToDisplayGraph(projection)</c>.
/// </summary>
public static class NodeProjectionExtensions
{
    public static DisplayGraph ToDisplayGraph<TNode>(
        this TNode root,
        INodeProjection<TNode> projection)
        where TNode : class
        => GraphWalk.Walk(root, projection);
}

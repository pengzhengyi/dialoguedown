namespace DialogueDown.Visualization.Display;

/// <summary>
/// A titled diagram for one compiler stage: a short <see cref="Description"/> of
/// what it shows, its <see cref="Nodes"/>, the <see cref="Edges"/> between them, and
/// optional <see cref="Tables"/> shown beside the graph (the semantic tab's speaker,
/// anchor, and jump-resolution tables; null for a plain graph stage). A tree is the
/// acyclic, single-parent case; a stage with shared nodes or cycles shows those as
/// reference edges.
/// </summary>
public sealed record DisplayGraph(
    string Title,
    string Description,
    IReadOnlyList<DisplayNode> Nodes,
    IReadOnlyList<DisplayEdge> Edges,
    IReadOnlyList<SemanticTable>? Tables = null,
    StageUnavailable? Unavailable = null)
{
    /// <summary>
    /// The named areas the nodes sit in — a scene, and later a file. Empty for a stage that has
    /// no grouping to show.
    /// </summary>
    public IReadOnlyList<DisplayRegion> Regions { get; init; } = [];

    /// <summary>
    /// Whether a <see cref="DisplayEdgeKind.Child"/> edge nests the child's source inside the
    /// parent's. True for a stage projected from a syntax tree, where a container's span is only
    /// its header and its true reach comes from its children. False for a stage whose child edges
    /// mark the spanning tree it is <em>drawn</em> with rather than what contains what — there a
    /// node's own span already covers everything it holds, and following those edges would stretch
    /// its reach along the flow instead.
    /// </summary>
    /// <remarks>
    /// A reader relies on this when jumping from a source selection into a stage: it decides
    /// whether the node revealed is found by subtree extent or by span alone.
    /// </remarks>
    public bool Nests { get; init; } = true;

    /// <summary>
    /// A placeholder for a stage the compile did not produce (a halted compile): it carries the
    /// stage's <paramref name="title"/> and <paramref name="description"/> but no graph, plus a
    /// <paramref name="reason"/> the reader sees on its disabled tab.
    /// </summary>
    public static DisplayGraph ForUnavailableStage(string title, string description, string reason) =>
        new(title, description, [], [], Tables: null, Unavailable: new StageUnavailable(reason));
}

/// <summary>
/// Why a stage's tab is disabled — its artifact was not produced. Carried in the report payload
/// so the client renders a disabled tab whose tooltip shows the <see cref="Reason"/>.
/// </summary>
public sealed record StageUnavailable(string Reason);

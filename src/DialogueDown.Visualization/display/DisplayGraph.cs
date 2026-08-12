namespace DialogueDown.Visualization;

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

namespace DialogueDown.Graph.Regions;

/// <summary>
/// A scene grouping: the blocks beneath one heading. It carries the heading's display
/// <see cref="Label"/> and its slug <see cref="Anchor"/>, the target a divert to the scene lands on.
/// </summary>
internal sealed record SceneRegion(
    RegionId Id,
    NodeId Entry,
    NodeId Exit,
    IReadOnlySet<NodeId> Members,
    IReadOnlyList<Region> Children,
    string Label,
    string Anchor) : Region(Id, Entry, Exit, Members, Children);

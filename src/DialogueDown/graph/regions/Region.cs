namespace DialogueDown.Graph.Regions;

/// <summary>
/// A named grouping of nodes overlaid on the flat graph: metadata, not flow. A grouping is a
/// region only when it is <b>addressable</b>: something outside it can name it and enter it, the
/// way a divert enters a scene by its anchor. The edges alone cannot recover that name. It
/// exposes the group's <see cref="Entry"/> and <see cref="Exit"/> nodes, the
/// <see cref="OwnNodes"/> it directly owns, and the <see cref="Subregions"/> nested within it.
/// Each concrete kind, such as a scene, adds only the metadata it owns.
/// </summary>
internal abstract record Region(
    RegionId Id,
    NodeId Entry,
    NodeId Exit,
    IReadOnlySet<NodeId> OwnNodes,
    IReadOnlyList<Region> Subregions);

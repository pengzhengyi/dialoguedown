namespace DialogueDown.Graph.Regions;

/// <summary>
/// A named grouping of nodes overlaid on the flat graph — metadata, not flow. It exposes the
/// group's <see cref="Entry"/> and <see cref="Exit"/> nodes (a divert enters at the entry, a
/// choice weaves back to the exit), the <see cref="Members"/> it spans, and the
/// <see cref="Subregions"/> nested within it. Each concrete kind — a scene, a block-control arm —
/// adds only the metadata it owns.
/// </summary>
internal abstract record Region(
    RegionId Id,
    NodeId Entry,
    NodeId Exit,
    IReadOnlySet<NodeId> Members,
    IReadOnlyList<Region> Subregions);

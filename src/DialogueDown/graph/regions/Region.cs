namespace DialogueDown.Graph.Regions;

/// <summary>
/// A named grouping of nodes overlaid on the flat graph — metadata, not flow. A grouping earns a
/// region only when it is <b>addressable</b>: something outside it can name it and enter it, the
/// way a divert enters a scene by its anchor. That naming metadata is what the edges cannot
/// recover, so it is overlaid here; a grouping derivable from the flow alone stays a query over
/// the graph instead. It exposes the group's <see cref="Entry"/> and <see cref="Exit"/> nodes,
/// the <see cref="OwnNodes"/> it directly owns, and the <see cref="Subregions"/> nested within
/// it. Each concrete kind — a scene now, a file later — adds only the metadata it owns.
/// </summary>
internal abstract record Region(
    RegionId Id,
    NodeId Entry,
    NodeId Exit,
    IReadOnlySet<NodeId> OwnNodes,
    IReadOnlyList<Region> Subregions);

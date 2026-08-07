namespace DialogueDown.Graph.Regions;

/// <summary>
/// A region's identity in a <see cref="DialogueGraph"/>'s grouping overlay. Like a
/// <see cref="NodeId"/>, it is an opaque handle rather than a positional index.
/// </summary>
internal readonly record struct RegionId(int Value);

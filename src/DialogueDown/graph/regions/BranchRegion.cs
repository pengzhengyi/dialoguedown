using DialogueDown.Script.Ast;

namespace DialogueDown.Graph.Regions;

/// <summary>
/// One arm of a block control: the blocks it plays when taken. Its <see cref="Guard"/> is the
/// arm's condition — an <c>if</c> or <c>elseif</c> carries one; the <c>else</c> arm's is null.
/// </summary>
internal sealed record BranchRegion(
    RegionId Id,
    NodeId Entry,
    NodeId Exit,
    IReadOnlySet<NodeId> Members,
    IReadOnlyList<Region> Children,
    Condition? Guard) : Region(Id, Entry, Exit, Members, Children);

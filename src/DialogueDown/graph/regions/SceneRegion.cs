using DialogueDown.Script.Ast;

namespace DialogueDown.Graph.Regions;

/// <summary>
/// A scene grouping: the blocks beneath one heading. It carries the heading's displayable
/// <see cref="Label"/> fragments and its slug <see cref="Anchor"/> — the target a divert to the
/// scene lands on. The label stays fragments so each platform renders its styling in its own way.
/// </summary>
internal sealed record SceneRegion(
    RegionId Id,
    NodeId Entry,
    NodeId Exit,
    IReadOnlySet<NodeId> Members,
    IReadOnlyList<Region> Subregions,
    IReadOnlyList<InlineFragment> Label,
    string Anchor) : Region(Id, Entry, Exit, Members, Subregions);

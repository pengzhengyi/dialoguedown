using DialogueDown.Graph.Builder;
using DialogueDown.Graph.Regions;
using DialogueDown.Script.Semantics;

namespace DialogueDown.Graph.Passes;

/// <summary>
/// Projects the scene tree into the graph's grouping overlay: one nested scene region per scene,
/// each exposing its entry and exit nodes and the nodes it directly owns. Runs after node
/// creation, once every block has an id.
/// </summary>
internal sealed class RegionPass : GraphBuildPass
{
    protected override void ApplyCore(GraphDraft draft, GraphBuildContext context)
    {
        var ids = new RegionIdSequence();
        foreach (var scene in context.Semantics.SceneRoot.Children)
        {
            if (BuildRegion(scene, draft, ids) is { } region)
            {
                draft.AddRegion(region);
            }
        }
    }

    private static SceneRegion? BuildRegion(Scene scene, GraphDraft draft, RegionIdSequence ids)
    {
        // A bare heading owns no content nodes, so it bounds no region.
        var nodes = scene.DocumentOrder().Select(draft.IdOf).ToArray();
        if (nodes.Length == 0)
        {
            return null;
        }

        var subregions = scene.Children
            .Select(child => BuildRegion(child, draft, ids))
            .OfType<Region>()
            .ToArray();

        return new SceneRegion(
            ids.Next(),
            Entry: nodes[0],
            Exit: nodes[^1],
            Members: scene.Blocks.Select(draft.IdOf).ToHashSet(),
            Subregions: subregions,
            Label: scene.Heading!.Title,
            Anchor: scene.Anchor!);
    }
}

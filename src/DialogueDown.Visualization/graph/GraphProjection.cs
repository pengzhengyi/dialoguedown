using DialogueDown.Graph;
using DialogueDown.Graph.Edges;
using DialogueDown.Graph.Nodes;
using DialogueDown.Graph.Regions;
using DialogueDown.Script.Ast;

namespace DialogueDown.Visualization.Graph;

/// <summary>
/// Projects a <see cref="DialogueGraph"/> into the Dialogue Graph tab's payload: one display node
/// per graph node and one display edge per graph edge, each labeled by the kind that gives it
/// meaning. Unlike the stage tabs before it, this one does not walk a tree from a root — it emits
/// every node in the graph's own order, so a node nothing reaches still appears. That orphan is
/// unreachable content the writer likely did not intend, and this is the tab where it shows.
/// Emitting in graph order also keeps a display id equal to the compiler's own node id.
/// </summary>
internal sealed class GraphProjection
{
    internal const string StageTitle = "Dialogue Graph";

    internal const string StageDescription =
        "The compiled flow a runtime walks: every block as a node, joined by the edges that lead "
        + "between them — fall-through, jumps, choice arms, and conditional branches. A node "
        + "nothing points at is content the script never reaches.";

    private const string SpeechCategory = "speech";
    private const string CallCategory = "call";
    private const string StructureCategory = "structure";
    private const string TerminalCategory = "terminal";

    /// <summary>
    /// A placeholder for the stage when the compile produced no graph, carrying the stage's title
    /// and description with no nodes.
    /// </summary>
    public static DisplayGraph Unavailable(string reason) =>
        DisplayGraph.ForUnavailableStage(StageTitle, StageDescription, reason);

    /// <summary>The graph's nodes and edges prepared for display.</summary>
    public DisplayGraph Project(DialogueGraph graph, string source)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(source);

        var sceneByNode = ScenesByNode(graph.Regions);
        var nodes = graph.Nodes.Select(node => Describe(node, source, sceneByNode)).ToArray();
        var edges = graph.Nodes
            .SelectMany(node => node.Out.Select(edge => Describe(node, edge)))
            .ToArray();

        return new DisplayGraph(StageTitle, StageDescription, nodes, edges);
    }

    private static DisplayNode Describe(
        DialogueNode node, string source, IReadOnlyDictionary<NodeId, string> sceneByNode)
    {
        var (label, category) = LabelOf(node);
        return new DisplayNode(
            DisplayId(node.Id),
            label,
            Attributes(node, sceneByNode),
            Source: Slice(source, node),
            Category: category,
            TypeName: TypeNameOf(node))
        {
            Span = SpanOf(node),
        };
    }

    // A node reads as what a writer recognizes: a line by what is said, a branching node by how
    // many ways it leads, the sentinel by its name.
    private static (string Label, string Category) LabelOf(DialogueNode node) => node switch
    {
        LineNode line => (LineLabel(line), SpeechCategory),
        ControlNode control => (ControlLabel(control), CallCategory),
        ChoiceNode choice => ($"Choice ({Count(choice.Out, "option", "options")})", StructureCategory),
        RandomChoiceNode random => ($"Random choice ({Count(random.Out, "option", "options")})", StructureCategory),
        BranchNode branch => ($"Conditional ({Count(branch.Out, "branch", "branches")})", StructureCategory),
        EndNode => ("End", TerminalCategory),
        _ => (node.GetType().Name, StructureCategory),
    };

    private static string LineLabel(LineNode line)
    {
        var speech = InlineText.Of(line.Speech).Trim();
        return line.Speaker.Name is { } name ? $"{name}: {speech}" : speech;
    }

    // A control line runs effects, jumps, or both. Naming its effects tells a reader what it does;
    // an effectless one only diverts, so it says so rather than reading as an empty node.
    private static string ControlLabel(ControlNode control) =>
        control.Effects.Count > 0
            ? string.Join(", ", control.Effects.Select(EffectText))
            : "(jump)";

    private static string EffectText(GameCall call) => call switch
    {
        DefaultCommand command => $"({command.Action})",
        CustomCommand custom => $"{custom.Name}(…)",
        _ => call.GetType().Name,
    };

    // Both forms are spelled out: English pluralization is not a suffix rule ("branches", not
    // "branchs"), and a label a reader sees is not the place to be approximately right.
    private static string Count(IReadOnlyList<Edge> edges, string singular, string plural) =>
        edges.Count == 1 ? $"1 {singular}" : $"{edges.Count} {plural}";

    private static string? TypeNameOf(DialogueNode node) => node switch
    {
        LineNode => "Line",
        ControlNode => "Control",
        _ => null,
    };

    private static IReadOnlyList<DisplayAttribute> Attributes(
        DialogueNode node, IReadOnlyDictionary<NodeId, string> sceneByNode)
    {
        var attributes = new List<DisplayAttribute>();
        if (sceneByNode.TryGetValue(node.Id, out var scene))
        {
            attributes.Add(new DisplayAttribute("scene", scene));
        }

        if (node is IGuardedNode { Guard: { } guard })
        {
            attributes.Add(new DisplayAttribute("guard", $"{guard.Key}?"));
        }

        return attributes;
    }

    // An edge reads as the kind of route it is, so a reader tells a fall-through from a jump or a
    // chosen arm at a glance. Succession is the default flow and needs no word for it.
    private static DisplayEdge Describe(DialogueNode from, Edge edge) =>
        new(DisplayId(from.Id), DisplayId(edge.Target), DisplayEdgeKind.Child);

    // The scene each node belongs to. A region is metadata rather than flow, so it rides along as
    // an attribute instead of becoming an edge that would imply control enters a grouping.
    private static IReadOnlyDictionary<NodeId, string> ScenesByNode(RegionTree regions)
    {
        var sceneByNode = new Dictionary<NodeId, string>();
        foreach (var region in Descendants(regions.Roots))
        {
            if (region is not SceneRegion scene)
            {
                continue;
            }

            foreach (var id in scene.OwnNodes)
            {
                sceneByNode[id] = InlineText.Of(scene.Label).Trim();
            }
        }

        return sceneByNode;
    }

    private static IEnumerable<Region> Descendants(IReadOnlyList<Region> regions)
    {
        foreach (var region in regions)
        {
            yield return region;
            foreach (var nested in Descendants(region.Subregions))
            {
                yield return nested;
            }
        }
    }

    private static string DisplayId(NodeId id) => $"n{id.Value}";

    // A synthetic node owns no source text, so it has no slice and no span to reveal.
    private static string? Slice(string source, DialogueNode node) =>
        node.Span.Length > 0 ? source.Substring(node.Span.Start, node.Span.Length) : null;

    private static DisplaySpan? SpanOf(DialogueNode node) =>
        node.Span.Length > 0 ? new DisplaySpan(node.Span.Start, node.Span.End) : null;
}

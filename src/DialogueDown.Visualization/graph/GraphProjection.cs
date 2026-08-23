using DialogueDown.Graph;
using DialogueDown.Graph.Edges;
using DialogueDown.Graph.Nodes;
using DialogueDown.Graph.Regions;
using DialogueDown.Script.Ast;
using DialogueDown.Visualization.Display;

namespace DialogueDown.Visualization.Graph;

/// <summary>
/// Projects a <see cref="DialogueGraph"/> into the Dialogue Graph tab's payload: one display node
/// per graph node and one display edge per graph edge.
///
/// <para>The report lays every stage out as a <b>tree</b>, so a graph — which has back-edges and
/// nodes reached from several places — has to name one parent per node. A walk from the entry
/// picks that spanning tree: the first edge to reach a node is its parent, and every other edge
/// is a <see cref="DisplayEdgeKind.Reference"/> the layout draws without following. A cycle is
/// therefore an ordinary reference back to an earlier node.</para>
///
/// <para>Every node appears, not only the ones the walk reaches. A node nothing points at is
/// unreachable content the writer likely did not intend, and this is the tab where it shows. It is
/// placed after the block before it in the script, so it reads where a reader looks for it, and
/// the link that places it is drawn as placement rather than as a route. The entry is the one node
/// with no parent, so a run starts at the leftmost thing on screen.</para>
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

    // Edge categories reuse the node palette, so a concept keeps one color across the report: a
    // divert is colored like the jump it came from, an option like the choice it leaves.
    private const string SuccessionCategory = "break";
    private const string DivertCategory = "jump";
    private const string OptionCategory = "choice";
    private const string BranchCategory = "control";

    // Not a route: the link that places a node the flow never reaches.
    private const string PlacementCategory = "deferred";

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

        var sceneByNode = ScenesByNode(graph.Regions, graph.Nodes);
        var layout = SpanningTree.Of(graph);

        var nodes = graph.Nodes.Select(node => Describe(node, source, sceneByNode)).ToArray();

        var edges = graph.Nodes
            .SelectMany(node => node.Out.Select(edge => Describe(node, edge, layout)))
            .Concat(layout.Placements.Select(Place))
            .ToArray();

        return new DisplayGraph(StageTitle, StageDescription, nodes, edges)
        {
            Regions = RegionsOf(graph.Regions),
        };
    }

    private static DisplayNode Describe(
        DialogueNode node, string source, IReadOnlyDictionary<NodeId, string> sceneByNode)
    {
        var (label, category) = LabelOf(node);
        return new DisplayNode(
            DisplayId(node.Id),
            label,
            Attributes(node),
            Source: Slice(source, node),
            Category: category,
            TypeName: TypeNameOf(node))
        {
            Span = SpanOf(node),
            // A region is drawn around the nodes that share it, so it is named once there rather
            // than repeated as a line of text under every one of them.
            Region = sceneByNode.GetValueOrDefault(node.Id),
        };
    }

    // A node reads as what a writer recognizes: a line by what is said, a branching node by what
    // kind of branch it is, the sentinel by its name. How many ways it leads is not spelled out —
    // the graph already draws one edge per way, and the label would only repeat the picture.
    private static (string Label, string Category) LabelOf(DialogueNode node) => node switch
    {
        LineNode line => (LineLabel(line), SpeechCategory),
        ControlNode control => (ControlLabel(control), CallCategory),
        ChoiceNode => ("Choice", StructureCategory),
        RandomChoiceNode => ("Random choice", StructureCategory),
        BranchNode => ("Conditional", StructureCategory),
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

    private static string? TypeNameOf(DialogueNode node) => node switch
    {
        LineNode => "Line",
        ControlNode => "Control",
        _ => null,
    };

    private static IReadOnlyList<DisplayAttribute> Attributes(DialogueNode node)
    {
        var attributes = new List<DisplayAttribute>();
        if (node is IConditionalNode { Condition: { } condition })
        {
            attributes.Add(new DisplayAttribute("condition", $"{condition.Key}?"));
        }

        return attributes;
    }

    // An edge the walk followed to first reach its target is that node's parent in the layout;
    // every other edge — a weave-back, a jump to an earlier scene — is drawn as a reference so the
    // tree stays a tree while the flow stays complete. Its category says what kind of route it is,
    // so a reader tells a fall-through from a jump or a chosen arm by color.
    private static DisplayEdge Describe(DialogueNode from, Edge edge, SpanningTree layout) =>
        new(
            DisplayId(from.Id),
            DisplayId(edge.Target),
            layout.IsParentOf(from.Id, edge.Target) ? DisplayEdgeKind.Child : DisplayEdgeKind.Reference)
        {
            Category = CategoryOf(edge),
            Label = LabelOf(edge),
        };

    // Scaffolding, not flow: it says where an unreachable node sits, and nothing travels it.
    private static DisplayEdge Place((NodeId From, NodeId To) placement) =>
        new(DisplayId(placement.From), DisplayId(placement.To), DisplayEdgeKind.Child)
        {
            Category = PlacementCategory,
        };

    // Only a jump carries words of its own. A fall-through was never written down, and an option
    // reads as the speech it leads to, so neither has a label to show.
    private static string? LabelOf(Edge edge) =>
        edge is DivertEdge divert && InlineText.Of(divert.Label).Trim() is { Length: > 0 } label
            ? label
            : null;

    private static string CategoryOf(Edge edge) => edge switch
    {
        SuccessionEdge => SuccessionCategory,
        DivertEdge => DivertCategory,
        OptionEdge or RandomOptionEdge => OptionCategory,
        BranchEdge => BranchCategory,
        _ => SuccessionCategory,
    };

    /// <summary>
    /// The regions the stage draws, each naming itself and pointing back at the text that
    /// declares it — a scene's heading — so a reader can be taken to where the region begins.
    /// </summary>
    private static IReadOnlyList<DisplayRegion> RegionsOf(RegionTree regions) =>
        regions.All()
            .OfType<SceneRegion>()
            .Select(scene => new DisplayRegion(
                InlineText.Of(scene.Label).Trim(), "Scene", scene.Anchor)
            {
                Span = HeadingSpan(scene),
            })
            .ToArray();

    // A scene is declared by its heading, and the heading's own fragments carry the only spans
    // that name it. Their reach is the title text — where a reader expects to land.
    private static DisplaySpan? HeadingSpan(SceneRegion scene)
    {
        if (scene.Label.Count == 0)
        {
            return null;
        }

        var start = scene.Label.Min(fragment => fragment.Span.Start);
        var end = scene.Label.Max(fragment => fragment.Span.End);
        return new DisplaySpan(start, end);
    }

    /// <summary>
    /// The scene each node reads as belonging to.
    /// </summary>
    /// <remarks>
    /// A scene owns the blocks written directly beneath its heading, so a line nested inside a
    /// choice arm is not among its <c>OwnNodes</c> — yet a reader plainly considers it part of the
    /// scene it was written under. Document order settles it: a heading opens a scene and
    /// everything after it belongs there until the next heading, so an unowned node inherits the
    /// scene of the text above it. Nodes written before any heading belong to no scene, and say so
    /// by having none.
    /// </remarks>
    private static IReadOnlyDictionary<NodeId, string> ScenesByNode(
        RegionTree regions, IReadOnlyList<DialogueNode> nodes)
    {
        var owned = new Dictionary<NodeId, string>();
        foreach (var region in regions.All())
        {
            if (region is not SceneRegion scene)
            {
                continue;
            }

            foreach (var id in scene.OwnNodes)
            {
                owned[id] = InlineText.Of(scene.Label).Trim();
            }
        }

        var sceneByNode = new Dictionary<NodeId, string>();
        string? current = null;
        foreach (var node in nodes)
        {
            if (owned.TryGetValue(node.Id, out var scene))
            {
                current = scene;
            }

            if (current is not null)
            {
                sceneByNode[node.Id] = current;
            }
        }

        return sceneByNode;
    }

    private static string DisplayId(NodeId id) => $"n{id.Value}";

    // A synthetic node owns no source text, so it has no slice and no span to reveal.
    private static string? Slice(string source, DialogueNode node) =>
        node.Span.Length > 0 ? source.Substring(node.Span.Start, node.Span.Length) : null;

    private static DisplaySpan? SpanOf(DialogueNode node) =>
        node.Span.Length > 0 ? new DisplaySpan(node.Span.Start, node.Span.End) : null;
}

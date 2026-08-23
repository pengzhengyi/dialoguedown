using DialogueDown.Graph;
using DialogueDown.Graph.Edges;
using DialogueDown.Graph.Nodes;
using DialogueDown.Script.Ast;

namespace DialogueDown.Tests.Support;

/// <summary>Assertions over a built <see cref="DialogueGraph"/>, its nodes, and their edges.</summary>
internal static class GraphAssert
{
    /// <summary>Asserts the edge is a succession to <paramref name="target"/>.</summary>
    public static SuccessionEdge AssertSuccession(Edge edge, NodeId target)
    {
        var succession = Assert.IsType<SuccessionEdge>(edge);
        Assert.Equal(target, succession.Target);
        return succession;
    }

    /// <summary>Asserts the edge is a divert to <paramref name="target"/>.</summary>
    public static DivertEdge AssertDivert(Edge edge, NodeId target)
    {
        var divert = Assert.IsType<DivertEdge>(edge);
        Assert.Equal(target, divert.Target);
        return divert;
    }

    /// <summary>Asserts a label reads as <paramref name="words"/>.</summary>
    public static void AssertReads(IReadOnlyList<InlineFragment> label, string words) =>
        Assert.Equal(words, InlineText.Of(label));

    /// <summary>Asserts the edge is an option, and returns it.</summary>
    public static OptionEdge AssertOption(Edge edge) => Assert.IsType<OptionEdge>(edge);

    /// <summary>Asserts the node offers exactly these options, labelled in this order.</summary>
    public static void AssertOffers(DialogueNode node, params string[] labels)
    {
        ArgumentNullException.ThrowIfNull(node);
        Assert.Equal(labels, node.Out.Select(edge => InlineText.Of(AssertOption(edge).Label)));
    }

    /// <summary>Asserts the node's only out-edge is a succession to <paramref name="target"/>.</summary>
    /// <summary>
    /// Asserts the node's one fall-through leads to <paramref name="target"/>, beside whatever
    /// other routes it offers — unlike <see cref="AssertOnlySuccession"/>, which asserts the
    /// fall-through is the only edge the node has.
    /// </summary>
    public static void AssertFallsThroughTo(DialogueNode node, NodeId target)
    {
        ArgumentNullException.ThrowIfNull(node);
        AssertSuccession(Assert.Single(node.Out.OfType<SuccessionEdge>()), target);
    }

    /// <summary>Asserts the node has no fall-through, so control always leaves it another way.</summary>
    public static void AssertNoFallThrough(DialogueNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Assert.Empty(node.Out.OfType<SuccessionEdge>());
    }

    public static void AssertOnlySuccession(DialogueNode node, NodeId target)
    {
        ArgumentNullException.ThrowIfNull(node);
        AssertSuccession(Assert.Single(node.Out), target);
    }

    /// <summary>Asserts the node's only out-edge is a divert to <paramref name="target"/>.</summary>
    public static DivertEdge AssertOnlyDivert(DialogueNode node, NodeId target)
    {
        ArgumentNullException.ThrowIfNull(node);
        return AssertDivert(Assert.Single(node.Out), target);
    }

    /// <summary>Asserts the edge is conditional by <paramref name="key"/>.</summary>
    public static void AssertConditional(Edge edge, string key) =>
        Assert.Equal(key, Assert.IsAssignableFrom<IConditionalEdge>(edge).Condition?.Key);

    /// <summary>
    /// Asserts the edge is the branch arm tried at <paramref name="order"/>, conditional by
    /// <paramref name="condition"/> — null for the <c>else</c> arm, which is always taken when reached.
    /// </summary>
    public static void AssertBranch(Edge edge, int order, string? condition)
    {
        var branch = Assert.IsType<BranchEdge>(edge);
        Assert.Equal(order, branch.Order);
        Assert.Equal(condition, branch.Condition?.Key);
    }

    /// <summary>Asserts the node's content plays only under <paramref name="key"/>.</summary>
    public static void AssertConditional(DialogueNode node, string key) =>
        Assert.Equal(key, Assert.IsAssignableFrom<IConditionalNode>(node).Condition?.Key);

    /// <summary>Asserts the node's content always plays.</summary>
    public static void AssertUnconditional(DialogueNode node) =>
        Assert.Null(Assert.IsAssignableFrom<IConditionalNode>(node).Condition);

    /// <summary>Asserts the edge is one control may always take.</summary>
    public static void AssertUnconditional(Edge edge) =>
        Assert.Null(Assert.IsAssignableFrom<IConditionalEdge>(edge).Condition);

    /// <summary>Asserts the edge is a random arm weighted at <paramref name="percentage"/>.</summary>
    public static void AssertNumberWeight(Edge edge, double percentage) =>
        Assert.Equal(
            percentage,
            Assert.IsType<NumberWeight>(Assert.IsType<RandomOptionEdge>(edge).Weight).Percentage);

    /// <summary>Asserts the node's out-edges point at exactly <paramref name="targets"/>, in order.</summary>
    public static void AssertTargets(DialogueNode node, params NodeId[] targets)
    {
        ArgumentNullException.ThrowIfNull(node);
        Assert.Equal(targets, node.Out.Select(edge => edge.Target));
    }

    /// <summary>
    /// Asserts the graph holds a node answering to <paramref name="id"/>. The graph resolves an id
    /// by lookup, so an id it does not hold is not a malformed drawing but an exception thrown at
    /// whichever runtime is walking the flow. <paramref name="namedBy"/> says what pointed at the
    /// id, so a failure names the edge or endpoint at fault rather than the id alone.
    /// </summary>
    public static void AssertHoldsNode(DialogueGraph graph, NodeId id, string namedBy)
    {
        ArgumentNullException.ThrowIfNull(graph);

        Assert.True(
            Record.Exception(() => graph.Node(id)) is null,
            NamesANodeTheGraphDoesNotHold(namedBy, id));
    }

    /// <summary>
    /// Asserts no two of the graph's nodes answer to the same id. The id is how everything
    /// downstream names a node, and the lookup that resolves a shared one silently prefers
    /// whichever was indexed last.
    /// </summary>
    public static void AssertNodeIdsAreDistinct(DialogueGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        var distinct = graph.Nodes.Select(node => node.Id).Distinct().Count();
        Assert.True(
            distinct == graph.Nodes.Count,
            IdsAreShared(graph.Nodes.Count, distinct));
    }

    private static string NamesANodeTheGraphDoesNotHold(string namedBy, NodeId id) =>
        $"{namedBy} points at {id}, which the graph does not hold.";

    private static string IdsAreShared(int nodes, int distinct) =>
        $"{nodes} nodes carry only {distinct} distinct ids, so at least two answer to one id.";
}

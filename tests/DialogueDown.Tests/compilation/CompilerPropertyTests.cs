using CsCheck;
using DialogueDown.Compilation;
using DialogueDown.Graph;
using DialogueDown.Graph.Edges;
using DialogueDown.Graph.Nodes;
using DialogueDown.Script.Ast;
using DialogueDown.Tests.Support;

namespace DialogueDown.Tests.Compilation;

/// <summary>
/// Properties that must hold for <em>every</em> script, not only the ones an example test names.
/// </summary>
/// <remarks>
/// Each property generates scripts and checks its invariant on every one, shrinking a failure to a
/// script small enough to read. Sample counts stay modest so the properties run in the ordinary
/// suite.
/// </remarks>
public sealed class CompilerPropertyTests
{
    private const int Samples = 200;

    /// <summary>
    /// Every node in the Dialogue AST carries a span that addresses text the script contains.
    /// </summary>
    /// <remarks>
    /// A node's text is <c>source.Substring(Start, Length)</c>, so a span reaching past the end of
    /// the source throws wherever it is sliced.
    /// </remarks>
    [Fact]
    public void EveryDialogueAstNodeSpanAddressesTextThatExists() =>
        ForEveryScript(
            source =>
            {
                foreach (var node in DialogueAstNodesOf(source))
                {
                    SpanAssert.AssertAddressesTextThatExists(node.Span, source, Describe(node));
                }
            });

    /// <summary>
    /// Every node in the Dialogue AST claims text lying wholly within what its parent claims.
    /// </summary>
    /// <remarks>
    /// A tool finds the node under a cursor by descending into the child that contains it, so a
    /// child reaching outside its parent cannot be found that way. A synthetic node carries a
    /// zero-width span at its position inside its parent.
    /// </remarks>
    [Fact]
    public void EveryDialogueAstNodeSpanIsContainedInItsParents() =>
        ForEveryScript(
            source =>
            {
                foreach (var parent in DialogueAstNodesOf(source))
                {
                    foreach (var child in parent.Children())
                    {
                        SpanAssert.AssertContainedIn(
                            child.Span, parent.Span, Describe(child), Describe(parent));
                    }
                }
            });

    /// <summary>
    /// Every node in the Markdown AST carries a span that addresses text the script contains.
    /// </summary>
    /// <remarks>
    /// This is the stage where the spans originate: the front end adopts the locations Markdig
    /// reports.
    /// </remarks>
    [Fact]
    public void EveryMarkdownAstNodeSpanAddressesTextThatExists() =>
        ForEveryScript(
            source =>
            {
                var markdown = ScriptCompilerFactory.CreateDefault().Compile(source).Markdown;

                foreach (var (subject, span) in MarkdownSpans.Of(markdown))
                {
                    SpanAssert.AssertAddressesTextThatExists(span, source, subject);
                }
            });

    /// <summary>
    /// Every node in the dialogue graph carries a span that addresses text the script contains.
    /// </summary>
    /// <remarks>
    /// A graph node's span is set by a lowering pass, separately from the Dialogue AST's spans. A
    /// synthetic node owns no source text and carries a zero-width span, which is in range like
    /// any other.
    /// </remarks>
    [Fact]
    public void EveryGraphNodeSpanAddressesTextThatExists() =>
        ForEveryGraph(
            (graph, source) =>
            {
                foreach (var node in graph.Nodes)
                {
                    SpanAssert.AssertAddressesTextThatExists(node.Span, source, Describe(node));
                }
            });

    /// <summary>
    /// Every edge in the dialogue graph, and the graph's own entry and end, name a node the graph
    /// holds.
    /// </summary>
    /// <remarks>
    /// An edge names its destination by id, so nothing in the type system stops it naming a node
    /// that was never emitted.
    /// </remarks>
    [Fact]
    public void EveryEdgeLandsOnANodeTheGraphHolds() =>
        ForEveryGraph(
            (graph, _) =>
            {
                GraphAssert.AssertHoldsNode(graph, graph.Entry, "the graph's entry");
                GraphAssert.AssertHoldsNode(graph, graph.End, "the graph's end");

                foreach (var node in graph.Nodes)
                {
                    foreach (var edge in node.Out)
                    {
                        GraphAssert.AssertHoldsNode(graph, edge.Target, Describe(edge, node));
                    }
                }
            });

    /// <summary>
    /// No two nodes in the dialogue graph share an id.
    /// </summary>
    [Fact]
    public void NoTwoGraphNodesShareAnId() =>
        ForEveryGraph((graph, _) => GraphAssert.AssertNodeIdsAreDistinct(graph));

    /// <summary>
    /// Desugaring a script that has already been desugared leaves it unchanged.
    /// </summary>
    /// <remarks>
    /// Desugar's rules are normalizations, such as assembling a jump or filling in the speaker a
    /// line left implicit, so a second pass finds nothing left to change.
    /// </remarks>
    [Fact]
    public void DesugaringAnAlreadyDesugaredScriptChangesNothing() =>
        ForEveryScript(
            source =>
            {
                var once = Pipeline.UntilDesugared(source);
                var twice = Pipeline.Desugar(once.Document, source);

                DialogueAstAssert.AssertSameShape(once.Document, twice.Document);
            });

    /// <summary>
    /// Compiling any script returns a result — a success, or a failure carrying diagnostics — and
    /// never throws.
    /// </summary>
    [Fact]
    public void CompilingNeverThrows() =>
        ForEveryScript(source => ScriptCompilerFactory.CreateDefault().Compile(source));

    private static void ForEveryScript(Action<string> invariantHolds) =>
        ScriptGen.Script().Sample(invariantHolds, iter: Samples);

    // Only a script the compiler accepts has a graph; a rejected one is skipped.
    private static void ForEveryGraph(Action<DialogueGraph, string> invariantHolds) =>
        ForEveryScript(
            source =>
            {
                if (GraphOf(source) is { } graph)
                {
                    invariantHolds(graph, source);
                }
            });

    private static DialogueGraph? GraphOf(string source) =>
        ScriptCompilerFactory.CreateDefault().Compile(source) is CompilationSuccess success
            ? success.Graph
            : null;

    // The Dialogue AST reachable from a compile, root blocks included. ScriptDocument is a
    // container rather than a node, so the walk starts at its body.
    private static IEnumerable<ScriptNode> DialogueAstNodesOf(string source) =>
        ScriptCompilerFactory.CreateDefault()
            .Compile(source)
            .Script.Body
            .SelectMany(block => block.DescendantsAndSelf());

    private static string Describe(ScriptNode node) => node.GetType().Name;

    private static string Describe(DialogueNode node) => $"{node.GetType().Name} {node.Id}";

    private static string Describe(Edge edge, DialogueNode leaving) =>
        $"the {edge.GetType().Name} leaving {Describe(leaving)}";
}

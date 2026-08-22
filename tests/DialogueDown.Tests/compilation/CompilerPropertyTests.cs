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
/// The suite's example tests each pin one input to one expected output, which is the right way to
/// specify behavior. What they cannot state is an invariant quantified over all inputs, and a span
/// that points outside its own source is exactly that kind of defect: no single example is wrong,
/// but some unwritten one would be. These generate scripts instead and check the invariant on each,
/// shrinking a failure to a script small enough to read.
/// <para>
/// Sample counts are deliberately modest: these run in the ordinary suite, and a property that
/// makes the suite slow stops being run at all.
/// </para>
/// </remarks>
public sealed class CompilerPropertyTests
{
    private const int Samples = 200;

    /// <summary>
    /// Every node in the Dialogue AST carries a span that addresses text the script contains.
    /// </summary>
    /// <remarks>
    /// A span is a promise that <c>source.Substring(Start, Length)</c> is the node's own text, and
    /// consumers take it at its word: the report slices it for a snippet, the editor maps it to a
    /// range, a diagnostic points a caret at it. A span reaching past the end of the source turns
    /// each of those into an exception or a caret in the wrong place, and it surfaces far from the
    /// stage that produced it.
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
    /// Containment is what makes the tree navigable by position: a tool finds the node under a
    /// cursor by descending into whichever child contains it, so a child reaching outside its
    /// parent is unreachable by that walk. A synthetic node carries a zero-width span at the
    /// position it belongs to, which is contained by definition.
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
    /// This is the stage where the spans originate. The front end adopts the locations Markdig
    /// reports, and the report's own projection clamps them before slicing because they are not
    /// trusted to be in range — while two other consumers slice them raw. The disagreement is
    /// worth settling by measurement rather than by reading the code.
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
    /// This is the span the report slices unclamped to show a node's own text, and the one a
    /// debugger highlights to say where a run has paused. It is a different span from the Dialogue
    /// AST's — a lowering pass decides it — so the tree being sound says nothing about it. A
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
    /// An edge names its destination by id rather than by reference, so nothing in the type system
    /// stops it naming one that was never emitted. The graph resolves an id by lookup, which means
    /// a dangling edge is not a malformed drawing but an exception thrown at whichever runtime is
    /// walking the flow — arbitrarily far from the pass that dropped the node.
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
    /// No two nodes in the dialogue graph answer to the same id.
    /// </summary>
    /// <remarks>
    /// The id is how everything downstream names a node — an edge's destination, a debugger's
    /// breakpoint, the report's selection. Two nodes answering to one id make every one of those
    /// ambiguous, and the lookup that resolves it silently prefers whichever was indexed last.
    /// </remarks>
    [Fact]
    public void NoTwoGraphNodesShareAnId() =>
        ForEveryGraph((graph, _) => GraphAssert.AssertNodeIdsAreDistinct(graph));

    /// <summary>
    /// Desugaring a script that has already been desugared leaves it unchanged.
    /// </summary>
    /// <remarks>
    /// Desugar's rules are normalizations — assemble a jump, fill in the speaker a line left
    /// implicit — and a normalization that is not idempotent is one that has not finished: running
    /// it again would keep changing the tree, so its output depends on how many times it ran. That
    /// makes the stage unsafe to re-run, which a cache, an incremental recompile, or a later pass
    /// reusing the stage would all quietly rely on.
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
    /// <remarks>
    /// The compiler's contract is that it turns text into a result. An exception escaping it is a
    /// defect whatever the input, because the caller cannot then tell "your script is wrong" from
    /// "the compiler broke."
    /// </remarks>
    [Fact]
    public void CompilingNeverThrows() =>
        ForEveryScript(source => ScriptCompilerFactory.CreateDefault().Compile(source));

    // Every property below is quantified the same way — over generated scripts — so the quantifier
    // is named once here and each property is left stating only its invariant.
    private static void ForEveryScript(Action<string> invariantHolds) =>
        ScriptGen.Script().Sample(invariantHolds, iter: Samples);

    // Only a script the compiler accepts reaches the graph stage. One it rejects has no graph, and
    // so says nothing either way about an invariant quantified over graphs.
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

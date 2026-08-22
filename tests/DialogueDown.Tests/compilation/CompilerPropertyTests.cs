using CsCheck;
using DialogueDown.Common;
using DialogueDown.Compilation;
using DialogueDown.Graph;
using DialogueDown.Markdown;
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
    /// Every span a compile produces addresses text that exists.
    /// </summary>
    /// <remarks>
    /// A span is a promise that <c>source.Substring(Start, Length)</c> is the node's own text, and
    /// consumers take it at its word: the report slices it for a snippet, the editor maps it to a
    /// range, a diagnostic points a caret at it. A span reaching past the end of the source turns
    /// each of those into an exception or a caret in the wrong place, and it surfaces far from the
    /// stage that produced it.
    /// </remarks>
    [Fact]
    public void EverySpanAddressesTextThatExists() =>
        ScriptGen.Script().Sample(
            source =>
            {
                foreach (var node in NodesOf(source))
                {
                    Assert.True(
                        node.Span.Start >= 0
                            && node.Span.End >= node.Span.Start
                            && node.Span.End <= source.Length,
                        $"{node.GetType().Name} claims [{node.Span.Start}, {node.Span.End}) "
                            + $"of a {source.Length}-character source.");
                }
            },
            iter: Samples);

    /// <summary>
    /// A child never claims text outside its parent.
    /// </summary>
    /// <remarks>
    /// Containment is what makes the tree navigable by position: a tool finds the node under a
    /// cursor by descending into whichever child contains it, so a child reaching outside its
    /// parent is unreachable by that walk. A synthetic node carries a zero-width span at the
    /// position it belongs to, which is contained by definition.
    /// </remarks>
    [Fact]
    public void AChildsSpanIsContainedInItsParents() =>
        ScriptGen.Script().Sample(
            source =>
            {
                foreach (var parent in NodesOf(source))
                {
                    foreach (var child in parent.Children())
                    {
                        Assert.True(
                            child.Span.Start >= parent.Span.Start
                                && child.Span.End <= parent.Span.End,
                            $"{child.GetType().Name} [{child.Span.Start}, {child.Span.End}) "
                                + $"escapes its parent {parent.GetType().Name} "
                                + $"[{parent.Span.Start}, {parent.Span.End}).");
                    }
                }
            },
            iter: Samples);

    /// <summary>
    /// The same holds one stage earlier, in the Markdown AST.
    /// </summary>
    /// <remarks>
    /// This is the stage where the spans originate. The front end adopts the locations Markdig
    /// reports, and the report's own projection clamps them before slicing because they are not
    /// trusted to be in range — while two other consumers slice them raw. The disagreement is
    /// worth settling by measurement rather than by reading the code.
    /// </remarks>
    [Fact]
    public void EveryMarkdownSpanAddressesTextThatExists() =>
        ScriptGen.Script().Sample(
            source =>
            {
                var markdown = ScriptCompilerFactory.CreateDefault().Compile(source).Markdown;

                foreach (var (name, span) in MarkdownSpansOf(markdown))
                {
                    Assert.True(
                        span.Start >= 0 && span.End >= span.Start && span.End <= source.Length,
                        $"{name} claims [{span.Start}, {span.End}) "
                            + $"of a {source.Length}-character source.");
                }
            },
            iter: Samples);

    /// <summary>
    /// Compiling never throws: an invalid script is reported, not raised.
    /// </summary>
    /// <remarks>
    /// The compiler's contract is that it turns text into a result — a success, or a failure
    /// carrying diagnostics. An exception escaping it is a defect whatever the input, because the
    /// caller cannot then tell "your script is wrong" from "the compiler broke."
    /// </remarks>
    [Fact]
    public void CompilingNeverThrows() =>
        ScriptGen.Script().Sample(
            source => ScriptCompilerFactory.CreateDefault().Compile(source),
            iter: Samples);

    /// <summary>
    /// The same holds for the graph's nodes, which carry spans of their own.
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
        ScriptGen.Script().Sample(
            source =>
            {
                if (GraphOf(source) is not { } graph)
                {
                    return;
                }

                foreach (var node in graph.Nodes)
                {
                    Assert.True(
                        node.Span.Start >= 0
                            && node.Span.End >= node.Span.Start
                            && node.Span.End <= source.Length,
                        $"{node.GetType().Name} claims [{node.Span.Start}, {node.Span.End}) "
                            + $"of a {source.Length}-character source.");
                }
            },
            iter: Samples);

    /// <summary>
    /// Every edge lands on a node the graph holds, as do its entry and end.
    /// </summary>
    /// <remarks>
    /// An edge names its destination by id rather than by reference, so nothing in the type system
    /// stops it naming one that was never emitted. The graph resolves an id by lookup, which means
    /// a dangling edge is not a malformed drawing but an exception thrown at whichever runtime is
    /// walking the flow — arbitrarily far from the pass that dropped the node.
    /// </remarks>
    [Fact]
    public void EveryEdgeLandsOnANodeTheGraphHolds() =>
        ScriptGen.Script().Sample(
            source =>
            {
                if (GraphOf(source) is not { } graph)
                {
                    return;
                }

                var present = graph.Nodes.Select(node => node.Id).ToHashSet();
                Assert.Contains(graph.Entry, present);
                Assert.Contains(graph.End, present);

                foreach (var node in graph.Nodes)
                {
                    foreach (var edge in node.Out)
                    {
                        Assert.True(
                            present.Contains(edge.Target),
                            $"{edge.GetType().Name} leaving {node.GetType().Name} "
                                + $"{node.Id} points at {edge.Target}, which the graph does "
                                + "not hold.");
                    }
                }
            },
            iter: Samples);

    /// <summary>
    /// No two nodes share an id.
    /// </summary>
    /// <remarks>
    /// The id is how everything downstream names a node — an edge's destination, a debugger's
    /// breakpoint, the report's selection. Two nodes answering to one id make every one of those
    /// ambiguous, and the lookup that resolves it silently prefers whichever was indexed last.
    /// </remarks>
    [Fact]
    public void NoTwoNodesShareAnId() =>
        ScriptGen.Script().Sample(
            source =>
            {
                if (GraphOf(source) is not { } graph)
                {
                    return;
                }

                var distinct = graph.Nodes.Select(node => node.Id).Distinct().Count();
                Assert.True(
                    distinct == graph.Nodes.Count,
                    $"{graph.Nodes.Count} nodes carry only {distinct} distinct ids.");
            },
            iter: Samples);

    /// <summary>
    /// Desugaring an already-desugared script changes nothing.
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
        ScriptGen.Script().Sample(
            source =>
            {
                var once = Pipeline.UntilDesugared(source);
                var twice = Pipeline.Desugar(once.Document, source);
                Assert.Equal(Shape(once.Document), Shape(twice.Document));
            },
            iter: Samples);

    // The graph, when the compile reached it. A script the front end rejects has no graph, and
    // says nothing either way about the invariants below.
    private static DialogueGraph? GraphOf(string source) =>
        ScriptCompilerFactory.CreateDefault().Compile(source) is CompilationSuccess success
            ? success.Graph
            : null;

    // The AST records hold their children in lists, which compare by reference, so two separately
    // built trees are never equal however alike they are. Comparing a rendering instead: the walk
    // reaches every node, including those inside those lists, and each node prints its own kind,
    // position, and scalar members — so a speaker filled in or a jump indicator consumed shows up
    // as a difference.
    private static string Shape(ScriptDocument document) =>
        string.Join(
            "\n",
            document.Body
                .SelectMany(block => block.DescendantsAndSelf())
                .Select(node => $"{node.GetType().Name} [{node.Span.Start},{node.Span.End}) {node}"));

    // The Markdown AST has no shared walker, because nothing in the compiler needs one: each
    // stage knows the shapes it handles. A property does need one, so it lives here rather than
    // widening the library's surface for a test.
    private static IEnumerable<(string Name, SourceSpan Span)> MarkdownSpansOf(
        MarkdownDocument document) =>
        document.Blocks.SelectMany(BlockSpans);

    private static IEnumerable<(string, SourceSpan)> BlockSpans(MarkdownBlock block)
    {
        yield return (block.GetType().Name, block.Span);

        var children = block switch
        {
            Heading heading => heading.Inlines.SelectMany(InlineSpans),
            Paragraph paragraph => paragraph.Inlines.SelectMany(InlineSpans),
            QuoteBlock quote => quote.Blocks.SelectMany(BlockSpans),
            ListBlock list => list.Items.SelectMany(ItemSpans),
            _ => [],
        };

        foreach (var child in children)
        {
            yield return child;
        }
    }

    private static IEnumerable<(string, SourceSpan)> ItemSpans(ListItem item)
    {
        yield return (nameof(ListItem), item.Span);

        foreach (var child in item.Blocks.SelectMany(BlockSpans))
        {
            yield return child;
        }
    }

    private static IEnumerable<(string, SourceSpan)> InlineSpans(MarkdownInline inline)
    {
        yield return (inline.GetType().Name, inline.Span);

        var children = inline switch
        {
            EmphasisInline emphasis => emphasis.Children.SelectMany(InlineSpans),
            LinkInline link => link.Label.SelectMany(InlineSpans),
            ImageInline image => image.Alt.SelectMany(InlineSpans),
            _ => Enumerable.Empty<(string, SourceSpan)>(),
        };

        foreach (var child in children)
        {
            yield return child;
        }
    }

    // The Dialogue AST reachable from a compile, root blocks included. ScriptDocument is a
    // container rather than a node, so the walk starts at its body.
    private static IEnumerable<ScriptNode> NodesOf(string source) =>
        ScriptCompilerFactory.CreateDefault()
            .Compile(source)
            .Script.Body
            .SelectMany(block => block.DescendantsAndSelf());
}

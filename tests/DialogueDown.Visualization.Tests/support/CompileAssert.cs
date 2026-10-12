using DialogueDown.Compilation;
using DialogueDown.Markdown;
using DialogueDown.Script.Ast;

namespace DialogueDown.Visualization.Tests.Support;

/// <summary>
/// Compiles a script and returns the syntax nodes of one kind it holds, so a projection test
/// describes a node the parser built from real source rather than one built by hand.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="AssertCompiles{TNode}"/> and <see cref="AssertCompilesSingle{TNode}"/> search the
/// trees as written, roots included: the Markdown AST and the transpiled Dialogue AST. Their node
/// types never overlap, so the type asked for picks the tree. <see cref="AssertDesugarsSingle{TNode}"/>
/// searches the desugared Dialogue AST instead, the only tree that holds an assembled
/// <see cref="Jump"/> or a filled <see cref="DefaultSpeaker"/>; look there when the trees as
/// written hold none. Nodes come back in document order, a parent before its children.
/// </para>
/// <para>
/// The source must compile without a diagnostic, so every fixture is a script a writer could
/// ship. A shape these tests cannot get from a script, such as a span that runs past the end of
/// the text, is still built by hand, beside a comment saying why.
/// </para>
/// </remarks>
internal static class CompileAssert
{
    /// <summary>Asserts the source compiles cleanly and holds at least one node of the type given.</summary>
    /// <returns>Every node of that type, in document order.</returns>
    public static IReadOnlyList<TNode> AssertCompiles<TNode>(string source)
        where TNode : notnull
    {
        IReadOnlyList<TNode> nodes = [.. WrittenNodesIn(AssertCompilesCleanly(source)).OfType<TNode>()];
        Assert.NotEmpty(nodes);
        return nodes;
    }

    /// <summary>Asserts the source compiles cleanly and holds exactly one node of the type given.</summary>
    /// <returns>That node.</returns>
    public static TNode AssertCompilesSingle<TNode>(string source)
        where TNode : notnull =>
        Assert.Single(WrittenNodesIn(AssertCompilesCleanly(source)).OfType<TNode>());

    /// <summary>
    /// Asserts the source compiles cleanly and its desugared Dialogue AST holds exactly one node of
    /// the type given.
    /// </summary>
    /// <returns>That node.</returns>
    public static TNode AssertDesugarsSingle<TNode>(string source)
        where TNode : notnull
    {
        var document = AssertCompilesCleanly(source).Desugared.Document;
        return Assert.Single(
            ((IEnumerable<object>)[document, .. ScriptNodesIn(document)]).OfType<TNode>());
    }

    private static CompilationSuccess AssertCompilesCleanly(string source)
    {
        var result = Pipeline.Compilation(source);
        Assert.Empty(result.LocatedDiagnostics);
        return Assert.IsType<CompilationSuccess>(result);
    }

    private static IEnumerable<object> WrittenNodesIn(CompilationResult result) =>
    [
        result.Markdown,
        .. result.Markdown.Blocks.SelectMany(InBlock),
        result.Script,
        .. ScriptNodesIn(result.Script),
    ];

    private static IEnumerable<ScriptNode> ScriptNodesIn(ScriptDocument document) =>
        document.Body.SelectMany(block => block.DescendantsAndSelf());

    private static IEnumerable<object> InBlock(MarkdownBlock block) =>
    [
        block,
        .. block switch
        {
            Heading heading => heading.Inlines.SelectMany(InInline),
            Paragraph paragraph => paragraph.Inlines.SelectMany(InInline),
            QuoteBlock quote => quote.Blocks.SelectMany(InBlock),
            ListBlock list => list.Items.SelectMany(InItem),
            _ => [],
        },
    ];

    private static IEnumerable<object> InItem(ListItem item) =>
        [item, .. item.Blocks.SelectMany(InBlock)];

    private static IEnumerable<object> InInline(MarkdownInline inline) =>
    [
        inline,
        .. inline switch
        {
            EmphasisInline emphasis => emphasis.Children.SelectMany(InInline),
            LinkInline link => link.Label.SelectMany(InInline),
            ImageInline image => image.Alt.SelectMany(InInline),
            _ => [],
        },
    ];
}

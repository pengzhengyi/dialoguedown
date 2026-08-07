using DialogueDown.Diagnostics;
using DialogueDown.Graph;
using DialogueDown.Markdown;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Semantics;

namespace DialogueDown.Compilation;

/// <summary>
/// A compile that ran every stage without reporting an error: each artifact is present, so
/// nothing here is optional and nothing throws. Matching this type is how a caller learns the
/// compile produced what it needs — above all the <see cref="Graph"/>, the flow a runtime walks.
/// </summary>
public sealed record CompilationSuccess : CompilationResult
{
    internal CompilationSuccess(
        string source,
        MarkdownDocument markdown,
        ScriptDocument script,
        DesugaredScriptDocument desugared,
        SemanticModel semantics,
        DialogueGraph graph,
        IReadOnlyList<Diagnostic> diagnostics)
        : base(source, markdown, script, diagnostics)
    {
        ArgumentNullException.ThrowIfNull(desugared);
        ArgumentNullException.ThrowIfNull(semantics);
        ArgumentNullException.ThrowIfNull(graph);
        Desugared = desugared;
        Semantics = semantics;
        Graph = graph;
    }

    /// <summary>The desugared Dialogue AST — the desugar stage's artifact.</summary>
    internal DesugaredScriptDocument Desugared { get; }

    /// <summary>The semantic model — the semantic-analysis stage's artifact.</summary>
    internal SemanticModel Semantics { get; }

    /// <summary>The dialogue graph — the graph stage's artifact, and the flow a runtime walks.</summary>
    internal DialogueGraph Graph { get; }
}

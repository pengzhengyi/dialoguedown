using DialogueDown.Diagnostics;
using DialogueDown.Markdown;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Semantics;

namespace DialogueDown.Compilation;

/// <summary>
/// A compile that stopped before running every stage, carrying only the artifacts it reached.
/// Each factory names where the compile stopped and takes exactly the artifacts reached by then.
/// </summary>
public sealed record CompilationFailure : CompilationResult
{
    private CompilationFailure(
        string source,
        MarkdownDocument markdown,
        ScriptDocument script,
        DesugaredScriptDocument? desugared,
        SemanticModel? semantics,
        IReadOnlyList<Diagnostic> diagnostics)
        : base(source, markdown, script, diagnostics)
    {
        Desugared = desugared;
        Semantics = semantics;
    }

    /// <summary>The desugared Dialogue AST, or null when the compile stopped before desugaring.</summary>
    internal DesugaredScriptDocument? Desugared { get; }

    /// <summary>The semantic model, or null when the compile stopped before analysis.</summary>
    internal SemanticModel? Semantics { get; }

    /// <summary>
    /// A compile that stopped at the transpiler, whose errors leave the later stages reading
    /// material they cannot trust. Nothing past the Dialogue AST was produced.
    /// </summary>
    internal static CompilationFailure AtTranspile(
        string source,
        MarkdownDocument markdown,
        ScriptDocument script,
        IReadOnlyList<Diagnostic> diagnostics) =>
        new(source, markdown, script, desugared: null, semantics: null, diagnostics);

    /// <summary>
    /// A compile that ran through semantic analysis and reported an error, so the model it
    /// recovered does not describe what the writer wrote and no graph was built. Everything up to
    /// the semantic model was reached, and a tool can still show it.
    /// </summary>
    internal static CompilationFailure AtAnalysis(
        string source,
        MarkdownDocument markdown,
        ScriptDocument script,
        DesugaredScriptDocument desugared,
        SemanticModel semantics,
        IReadOnlyList<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(desugared);
        ArgumentNullException.ThrowIfNull(semantics);
        return new(source, markdown, script, desugared, semantics, diagnostics);
    }
}

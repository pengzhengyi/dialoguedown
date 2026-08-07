using DialogueDown.Diagnostics;
using DialogueDown.Markdown;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Desugar;
using DialogueDown.Script.Semantics;

namespace DialogueDown.Compilation;

/// <summary>
/// A compile that ran every stage: each artifact is present, so nothing here is optional and
/// nothing throws. Matching this type is how a caller learns the compile produced what it needs.
/// </summary>
public sealed record CompilationSuccess : CompilationResult
{
    internal CompilationSuccess(
        string source,
        MarkdownDocument markdown,
        ScriptDocument script,
        DesugaredScriptDocument desugared,
        SemanticModel semantics,
        IReadOnlyList<Diagnostic> diagnostics)
        : base(source, markdown, script, diagnostics)
    {
        ArgumentNullException.ThrowIfNull(desugared);
        ArgumentNullException.ThrowIfNull(semantics);
        Desugared = desugared;
        Semantics = semantics;
    }

    /// <summary>The desugared Dialogue AST — the desugar stage's artifact.</summary>
    internal DesugaredScriptDocument Desugared { get; }

    /// <summary>The semantic model — the semantic-analysis stage's artifact.</summary>
    internal SemanticModel Semantics { get; }
}

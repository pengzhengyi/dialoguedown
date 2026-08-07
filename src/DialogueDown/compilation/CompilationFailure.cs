using DialogueDown.Diagnostics;
using DialogueDown.Markdown;
using DialogueDown.Script.Ast;

namespace DialogueDown.Compilation;

/// <summary>
/// A compile that stopped before running every stage, carrying only the artifacts it reached. It
/// is named by where it stopped rather than by a stage field beside optional artifacts, so a
/// combination no compile can reach cannot be constructed.
/// </summary>
public sealed record CompilationFailure : CompilationResult
{
    private CompilationFailure(
        string source,
        MarkdownDocument markdown,
        ScriptDocument script,
        IReadOnlyList<Diagnostic> diagnostics)
        : base(source, markdown, script, diagnostics)
    {
    }

    /// <summary>
    /// A compile that stopped at the transpiler, whose errors leave the later stages reading
    /// material they cannot trust. Nothing past the Dialogue AST was produced.
    /// </summary>
    internal static CompilationFailure AtTranspile(
        string source,
        MarkdownDocument markdown,
        ScriptDocument script,
        IReadOnlyList<Diagnostic> diagnostics) =>
        new(source, markdown, script, diagnostics);
}

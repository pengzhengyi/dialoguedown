using DialogueDown.Diagnostics;
using DialogueDown.Markdown;
using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Transpiler;

/// <summary>
/// Transpiles a Markdown AST into the Dialogue AST.
/// </summary>
internal interface IScriptTranspiler
{
    /// <summary>
    /// Transpiles <paramref name="document"/> into a <see cref="ScriptDocument"/>, reporting
    /// into the sink of <paramref name="context"/>. Text and spans are read from the Markdown AST.
    /// </summary>
    ScriptDocument Transpile(MarkdownDocument document, DiagnosticsContext context);
}

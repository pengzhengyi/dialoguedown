using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Desugar;

/// <summary>
/// Desugars a transpiled Dialogue AST, applying the local normalizations the transpiler
/// leaves for later: jump assembly, control-line recognition, and the default-speaker fill.
/// </summary>
internal interface IScriptDesugarer
{
    /// <summary>
    /// Desugars <paramref name="document"/> into a <see cref="DesugaredScriptDocument"/>,
    /// reporting into the sink of <paramref name="context"/>.
    /// </summary>
    DesugaredScriptDocument Desugar(ScriptDocument document, DiagnosticsContext context);
}

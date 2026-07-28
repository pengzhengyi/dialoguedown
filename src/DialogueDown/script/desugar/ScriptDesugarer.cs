using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Desugar;

/// <summary>
/// The default <see cref="IScriptDesugarer"/>: it runs the <see cref="Desugarer"/> rewrite
/// over the document and wraps the result as a <see cref="DesugaredScriptDocument"/>.
/// </summary>
internal sealed class ScriptDesugarer : IScriptDesugarer
{
    private readonly Desugarer _desugarer = DesugarerFactory.CreateDefault();

    public DesugaredScriptDocument Desugar(ScriptDocument document, DiagnosticsContext context)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(context);

        // TODO(diagnostics): the context is validated but not yet read — desugaring works off
        // the tree and the spans it already carries. Report warnings into context.Diagnostics
        // when the producers land (e.g. a dangling arrow or multiple jumps).
        return new DesugaredScriptDocument(_desugarer.Desugar(document));
    }
}

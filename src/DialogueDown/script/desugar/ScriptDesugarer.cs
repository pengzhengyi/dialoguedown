using DialogueDown.Diagnostics;
using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Desugar;

/// <summary>
/// The default <see cref="IScriptDesugarer"/>: it runs the <see cref="Desugarer"/> rewrite
/// over the document and wraps the result as a <see cref="DesugaredScriptDocument"/>.
/// </summary>
internal sealed class ScriptDesugarer : IScriptDesugarer
{
    public DesugaredScriptDocument Desugar(ScriptDocument document, DiagnosticsContext context)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(context);

        // Built per compilation, not once per process: a desugar rule reports into this
        // compilation's sink, and this class is registered as a singleton, so a rule kept in a
        // field would outlive the compilation it reports for.
        var desugarer = DesugarerFactory.CreateDefault(context.Diagnostics);
        return new DesugaredScriptDocument(desugarer.Desugar(document));
    }
}

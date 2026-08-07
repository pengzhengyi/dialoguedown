using DialogueDown.Diagnostics;
using DialogueDown.Script.Semantics;

namespace DialogueDown.Graph;

/// <summary>
/// Lowers a <see cref="SemanticModel"/> into a <see cref="DialogueGraph"/> — the compiler stage
/// after semantic analysis. Mirrors <c>ISemanticAnalyzer</c>: a pure function of the model, with
/// no I/O and no engine dependency.
/// </summary>
internal interface IDialogueGraphBuilder
{
    /// <summary>
    /// Builds the dialogue graph for <paramref name="model"/>. <paramref name="context"/> is the
    /// stage's diagnostics sink.
    /// </summary>
    DialogueGraph Build(SemanticModel model, DiagnosticsContext context);
}

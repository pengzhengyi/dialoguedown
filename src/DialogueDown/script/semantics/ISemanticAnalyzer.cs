using DialogueDown.Diagnostics;
using DialogueDown.Script.Desugar;

namespace DialogueDown.Script.Semantics;

/// <summary>
/// Analyzes a desugared Dialogue AST into a <see cref="SemanticModel"/> — resolving speakers,
/// scenes and anchors, and jumps, and validating references.
/// </summary>
internal interface ISemanticAnalyzer
{
    /// <summary>
    /// Analyzes <paramref name="document"/> into a <see cref="SemanticModel"/>, reporting into
    /// the sink of <paramref name="context"/>.
    /// </summary>
    SemanticModel Analyze(DesugaredScriptDocument document, DiagnosticsContext context);
}

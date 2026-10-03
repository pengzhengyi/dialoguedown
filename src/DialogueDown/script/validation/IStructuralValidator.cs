using DialogueDown.Diagnostics;
using DialogueDown.Script.Desugar;

namespace DialogueDown.Script.Validation;

/// <summary>
/// The structural validation pass: it inspects a desugared document and reports structural
/// problems into a sink, running between desugar and semantic analysis.
/// </summary>
internal interface IStructuralValidator
{
    /// <summary>Validates <paramref name="document"/>, reporting findings into <paramref name="diagnostics"/>.</summary>
    void Validate(DesugaredScriptDocument document, IDiagnosticSink diagnostics);
}

using DialogueDown.Script.Ast;

namespace DialogueDown.Script.Desugar;

/// <summary>
/// One desugar normalization over the Dialogue AST: it rewrites the document and returns the
/// result, so rules compose into a pipeline. Each rule owns one normalization and is
/// unit-testable in isolation, so a rule can be added without touching the pipeline.
/// </summary>
internal interface IDesugarRule
{
    /// <summary>Applies this rule's normalization to <paramref name="document"/>.</summary>
    ScriptDocument Apply(ScriptDocument document);
}

using DialogueDown.Script.Desugar;
using DialogueDown.Script.Semantics;

namespace DialogueDown.Compilation;

/// <summary>
/// Reads what a compile <b>reached</b> — the artifacts it produced, whichever ending it came to.
/// Reaching a stage is not the same as succeeding: a compile that ran every stage and then
/// reported an error is a <see cref="CompilationFailure"/> that still carries a desugared tree
/// and a semantic model, which a tool describing a broken script needs. Matching
/// <see cref="CompilationSuccess"/> answers a different question — whether the compile produced
/// everything — so a caller that only wants to show a stage asks here instead.
/// </summary>
internal static class CompilationResultExtensions
{
    /// <summary>The desugared tree, or null when the compile stopped before desugaring.</summary>
    public static DesugaredScriptDocument? ReachedDesugared(this CompilationResult result) =>
        result switch
        {
            CompilationSuccess success => success.Desugared,
            CompilationFailure failure => failure.Desugared,
            _ => throw UnknownOutcome(result),
        };

    /// <summary>The semantic model, or null when the compile stopped before analysis.</summary>
    public static SemanticModel? ReachedSemantics(this CompilationResult result) =>
        result switch
        {
            CompilationSuccess success => success.Semantics,
            CompilationFailure failure => failure.Semantics,
            _ => throw UnknownOutcome(result),
        };

    private static NotSupportedException UnknownOutcome(CompilationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new NotSupportedException(
            $"There is no reading for the compilation outcome {result.GetType().Name}.");
    }
}

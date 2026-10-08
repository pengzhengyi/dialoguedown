namespace DialogueDown.Configuration;

/// <summary>
/// How far a compile proceeds after an error.
/// </summary>
public enum CompilationMode
{
    /// <summary>
    /// Stop at the first error, throwing a <c>DiagnosticException</c> that carries it — the quickest
    /// "is it broken?" answer.
    /// </summary>
    FailFast,

    /// <summary>
    /// Recover within a stage and collect every error; after the transpiler, halt if any error was
    /// reported, because its output does not reliably feed the next stage. The default.
    /// </summary>
    StageBoundary,

    /// <summary>
    /// Recover through every stage up to semantic analysis and collect everything, for the
    /// fullest picture.
    /// </summary>
    BestEffort,
}

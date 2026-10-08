namespace DialogueDown.Diagnostics;

/// <summary>
/// How serious a <see cref="Diagnostic"/> is, ordered so <see cref="Error"/> is the
/// worst: a producer or the result can ask "did anything fail?" by comparing against
/// <see cref="Error"/>, or "what is the worst?" by taking the maximum. Part of the public
/// diagnostic view (<see cref="LocatedDiagnostic"/>), so its members carry explicit, stable values.
/// </summary>
public enum DiagnosticSeverity
{
    /// <summary>A neutral note: nothing is wrong, but something is worth pointing out.</summary>
    Info = 0,

    /// <summary>The script compiles but is suspect: likely a mistake.</summary>
    Warning = 1,

    /// <summary>The script is invalid: the reported problem must be fixed.</summary>
    Error = 2,
}

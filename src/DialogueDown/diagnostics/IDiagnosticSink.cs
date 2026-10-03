namespace DialogueDown.Diagnostics;

/// <summary>
/// Where a producer reports a <see cref="Diagnostic"/> during one compilation, without knowing
/// how diagnostics are stored or shown.
/// </summary>
internal interface IDiagnosticSink
{
    /// <summary>
    /// Collect one diagnostic. Throws <see cref="ArgumentNullException"/> when it is <c>null</c>.
    /// </summary>
    void Report(Diagnostic diagnostic);
}

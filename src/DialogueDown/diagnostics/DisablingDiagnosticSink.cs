namespace DialogueDown.Diagnostics;

/// <summary>
/// An <see cref="IDiagnosticSink"/> decorator that marks a diagnostic
/// <see cref="Diagnostic.IsDisabled">disabled</see> when a project disabled its code, then forwards
/// it to an inner sink. An error is never marked, whatever its code, because disabling it would let
/// a broken script compile.
/// </summary>
internal sealed class DisablingDiagnosticSink : IDiagnosticSink
{
    private readonly IDiagnosticSink _inner;
    private readonly IReadOnlySet<string> _disabledCodes;

    /// <param name="inner">The sink every diagnostic is forwarded to.</param>
    /// <param name="disabledCodes">The disabled codes, compared exactly; callers pass them upper-case.</param>
    public DisablingDiagnosticSink(IDiagnosticSink inner, IReadOnlySet<string> disabledCodes)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(disabledCodes);
        _inner = inner;
        _disabledCodes = disabledCodes;
    }

    public void Report(Diagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        _inner.Report(IsDisabled(diagnostic) ? diagnostic with { IsDisabled = true } : diagnostic);
    }

    private bool IsDisabled(Diagnostic diagnostic) =>
        !diagnostic.IsError && _disabledCodes.Contains(diagnostic.Descriptor.Code);
}

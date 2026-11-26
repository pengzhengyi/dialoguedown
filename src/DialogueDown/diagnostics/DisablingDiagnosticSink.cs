namespace DialogueDown.Diagnostics;

/// <summary>
/// An <see cref="IDiagnosticSink"/> decorator that marks a diagnostic
/// <see cref="Diagnostic.IsDisabled">disabled</see> when its code is in a set, then forwards it to
/// an inner sink.
/// </summary>
/// <remarks>
/// Codes are compared exactly, so callers pass them upper-case. An error is never marked, whatever
/// its code, because disabling it would let a broken script compile.
/// </remarks>
internal sealed class DisablingDiagnosticSink : IDiagnosticSink
{
    private readonly IDiagnosticSink _inner;
    private readonly IReadOnlySet<string> _disabledCodes;

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
        _inner.Report(ShouldMark(diagnostic) ? diagnostic with { IsDisabled = true } : diagnostic);
    }

    private bool ShouldMark(Diagnostic diagnostic) =>
        !diagnostic.IsError && _disabledCodes.Contains(diagnostic.Descriptor.Code);
}

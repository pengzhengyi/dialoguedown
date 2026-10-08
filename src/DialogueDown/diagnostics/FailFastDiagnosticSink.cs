using DialogueDown.Diagnostics.Errors;

namespace DialogueDown.Diagnostics;

/// <summary>
/// A fail-fast <see cref="IDiagnosticSink"/> decorator: it forwards every reported diagnostic to an
/// inner sink, but the first <see cref="DiagnosticSeverity.Error"/> also throws a
/// <see cref="DiagnosticException"/> carrying it, so a compile stops at the first error while still
/// collecting the warnings that preceded it.
/// </summary>
internal sealed class FailFastDiagnosticSink : IDiagnosticSink
{
    private readonly IDiagnosticSink _inner;

    public FailFastDiagnosticSink(IDiagnosticSink inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
    }

    public void Report(Diagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        _inner.Report(diagnostic);
        if (diagnostic.IsError)
        {
            throw new DiagnosticException(diagnostic);
        }
    }
}

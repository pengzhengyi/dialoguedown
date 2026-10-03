namespace DialogueDown.Diagnostics;

/// <summary>
/// The concrete <see cref="IDiagnosticSink"/> for one compilation: it collects diagnostics as
/// producers report them and hands back an immutable snapshot in report order.
/// </summary>
internal sealed class DiagnosticBag : IDiagnosticSink
{
    private readonly List<Diagnostic> _reported = [];

    /// <summary>
    /// The reported diagnostics as a detached snapshot, in report order — a later report never
    /// changes a snapshot taken earlier, and the snapshot cannot be used to mutate the bag.
    /// </summary>
    public IReadOnlyList<Diagnostic> Diagnostics => _reported.ToArray();

    /// <summary>Whether any collected diagnostic is an <see cref="DiagnosticSeverity.Error"/>.</summary>
    public bool HasErrors => _reported.Any(diagnostic => diagnostic.IsError);

    public void Report(Diagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        _reported.Add(diagnostic);
    }
}

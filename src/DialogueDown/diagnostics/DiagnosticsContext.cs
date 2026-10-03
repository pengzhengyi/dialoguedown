namespace DialogueDown.Diagnostics;

/// <summary>
/// The per-compilation bundle passed to each stage for diagnostics: the original
/// <see cref="Source"/> a report is anchored to, and the <see cref="Diagnostics"/> sink to report
/// into. It carries the write-only sink, not the bag, so a stage reports but never reads what has
/// been collected.
/// </summary>
internal sealed class DiagnosticsContext
{
    public DiagnosticsContext(string source, IDiagnosticSink diagnostics)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(diagnostics);
        Source = source;
        Diagnostics = diagnostics;
    }

    /// <summary>The original script text, for slicing and locating diagnostics.</summary>
    public string Source { get; }

    /// <summary>The sink a stage reports diagnostics into during this compilation.</summary>
    public IDiagnosticSink Diagnostics { get; }
}

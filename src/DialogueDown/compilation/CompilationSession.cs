using DialogueDown.Configuration;
using DialogueDown.Diagnostics;

namespace DialogueDown.Compilation;

/// <summary>
/// The diagnostics state of one compilation: the sink each stage reports through (chosen from
/// the <see cref="CompilationPolicy"/>), the diagnostics it collects, and, in stage-boundary mode,
/// whether the compile should stop after a stage that reported an error. The compiler consults
/// it between stages.
/// </summary>
internal sealed class CompilationSession
{
    private readonly DiagnosticBag _diagnostics = new();
    private readonly CompilationMode _mode;

    private CompilationSession(string source, CompilationPolicy policy)
    {
        _mode = policy.Mode;
        Context = new DiagnosticsContext(source, CreateSink(policy, _diagnostics));
    }

    /// <summary>The context each stage reports through: the source and the sink.</summary>
    public DiagnosticsContext Context { get; }

    /// <summary>The diagnostics collected so far, in report order.</summary>
    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics.Diagnostics;

    /// <summary>Whether any stage has reported an error, so the script is not valid.</summary>
    public bool HasErrors => _diagnostics.HasErrors;

    /// <summary>
    /// Whether the compile should stop at the current stage boundary: true only when a
    /// stage-boundary compile has collected an error, whose recovery leaves the later stages
    /// unreliable. Fail-fast never reaches here (its sink already threw); best-effort never halts.
    /// </summary>
    public bool ShouldHalt => _mode == CompilationMode.StageBoundary && _diagnostics.HasErrors;

    /// <summary>Starts a session for compiling <paramref name="source"/> under <paramref name="policy"/>.</summary>
    public static CompilationSession Start(string source, CompilationPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(policy);
        return new CompilationSession(source, policy);
    }

    // The disabling sink is outermost, so the mode's sink and the bag both see the mark.
    private static IDiagnosticSink CreateSink(CompilationPolicy policy, DiagnosticBag diagnostics)
    {
        IDiagnosticSink sink = policy.Mode switch
        {
            CompilationMode.FailFast => new FailFastDiagnosticSink(diagnostics),
            _ => diagnostics,
        };
        return policy.DisabledDiagnostics.Count == 0
            ? sink
            : new DisablingDiagnosticSink(sink, policy.DisabledDiagnostics);
    }
}

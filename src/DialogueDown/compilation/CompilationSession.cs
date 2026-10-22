using DialogueDown.Configuration;
using DialogueDown.Diagnostics;

namespace DialogueDown.Compilation;

/// <summary>The diagnostics state of one compilation, which the compiler consults between stages.</summary>
/// <remarks>
/// It holds the sink each stage reports through, built from the
/// <see cref="CompilationSessionOptions"/>; the diagnostics collected so far; and, in stage-boundary
/// mode, whether the compile should stop after a stage that reported an error.
/// </remarks>
internal sealed class CompilationSession
{
    private readonly DiagnosticBag _diagnostics = new();
    private readonly CompilationMode _mode;

    private CompilationSession(string source, CompilationSessionOptions options)
    {
        _mode = options.Mode;
        Context = new DiagnosticsContext(source, CreateSink(options, _diagnostics));
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

    /// <summary>Starts a session for compiling <paramref name="source"/> under <paramref name="options"/>.</summary>
    public static CompilationSession Start(string source, CompilationSessionOptions options)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(options);
        return new CompilationSession(source, options);
    }

    // The disabling sink is outermost, so the mode's sink and the bag both see the mark.
    private static DisablingDiagnosticSink CreateSink(CompilationSessionOptions options, DiagnosticBag diagnostics)
    {
        IDiagnosticSink sink = options.Mode switch
        {
            CompilationMode.FailFast => new FailFastDiagnosticSink(diagnostics),
            _ => diagnostics,
        };
        return new DisablingDiagnosticSink(sink, options.DisabledDiagnostics);
    }
}

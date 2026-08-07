using DialogueDown.Configuration;
using DialogueDown.Diagnostics;

namespace DialogueDown.Compilation;

/// <summary>
/// The diagnostic apparatus for one compilation: the sink each stage reports through (chosen from
/// the <see cref="CompilationMode"/>), the diagnostics it collects, and — for the stage-boundary
/// mode — whether the compile should stop after a stage that reported an error. It keeps this
/// policy out of the compiler, which only drives the stages and consults the session between them
/// (much like a compiler driver checks its session between passes).
/// </summary>
internal sealed class CompilationSession
{
    private readonly DiagnosticBag _diagnostics = new();
    private readonly CompilationMode _mode;

    private CompilationSession(string source, CompilationMode mode)
    {
        _mode = mode;
        Context = new DiagnosticsContext(source, CreateSink(mode, _diagnostics));
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

    /// <summary>Starts a session for compiling <paramref name="source"/> under <paramref name="mode"/>.</summary>
    public static CompilationSession Start(string source, CompilationMode mode)
    {
        ArgumentNullException.ThrowIfNull(source);
        return new CompilationSession(source, mode);
    }

    // The sink each stage reports through: fail-fast throws on the first error; the collecting
    // modes report straight into the bag. A new mode that needs a different sink adds a case.
    private static IDiagnosticSink CreateSink(CompilationMode mode, DiagnosticBag diagnostics) =>
        mode switch
        {
            CompilationMode.FailFast => new FailFastDiagnosticSink(diagnostics),
            _ => diagnostics,
        };
}

using DialogueDown.Diagnostics;
using DialogueDown.Markdown;
using DialogueDown.Script.Ast;

namespace DialogueDown.Compilation;

/// <summary>
/// The outcome of one compilation: the original <see cref="Source"/>, the front-end artifacts
/// every compile reaches, and the <see cref="Diagnostics"/> collected while compiling. Which
/// ending it is, is its type — a <see cref="CompilationSuccess"/> carrying every stage artifact,
/// or a <see cref="CompilationFailure"/> carrying how far the compile got. The stage artifacts
/// and the diagnostics are internal — they are the compiler's own types, still under active
/// design — so tooling that has friend access (the visualization project) can project them,
/// while a public caller sees the source, a <see cref="HasErrors"/> convenience, and the located
/// diagnostics.
/// </summary>
public abstract record CompilationResult
{
    private readonly Lazy<IReadOnlyList<LocatedDiagnostic>> _located;

    private protected CompilationResult(
        string source,
        MarkdownDocument markdown,
        ScriptDocument script,
        IReadOnlyList<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(markdown);
        ArgumentNullException.ThrowIfNull(script);
        ArgumentNullException.ThrowIfNull(diagnostics);
        Source = source;
        Markdown = markdown;
        Script = script;
        Diagnostics = diagnostics;
        _located = new Lazy<IReadOnlyList<LocatedDiagnostic>>(BuildLocatedDiagnostics);
    }

    /// <summary>The original script text this result was compiled from.</summary>
    public string Source { get; }

    /// <summary>Whether any collected diagnostic is an error — the script is not valid.</summary>
    public bool HasErrors => Diagnostics.Any(diagnostic => diagnostic.IsError);

    /// <summary>
    /// The located, rendered diagnostics for this compile — the public view a consumer displays:
    /// each carries its code, severity, final message, and one-based start/end position. Built once
    /// on first access (cached) by projecting the collected diagnostics through a
    /// <see cref="LineMap"/> over <see cref="Source"/>, in report order.
    /// </summary>
    public IReadOnlyList<LocatedDiagnostic> LocatedDiagnostics => _located.Value;

    /// <summary>The parsed Markdown AST — the front-end stage's artifact.</summary>
    internal MarkdownDocument Markdown { get; }

    /// <summary>The transpiled Dialogue AST — the transpiler stage's artifact.</summary>
    internal ScriptDocument Script { get; }

    /// <summary>The diagnostics collected while compiling, in report order.</summary>
    internal IReadOnlyList<Diagnostic> Diagnostics { get; }

    private IReadOnlyList<LocatedDiagnostic> BuildLocatedDiagnostics()
    {
        var map = new LineMap(Source);
        return [.. Diagnostics.Select(diagnostic => LocatedDiagnostic.Project(diagnostic, map))];
    }
}

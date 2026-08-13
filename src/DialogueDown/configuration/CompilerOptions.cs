namespace DialogueDown.Configuration;

/// <summary>
/// Immutable options that configure a single compile — the public umbrella a consumer builds, in
/// code or by loading a <c>dialogue.toml</c>, and hands to a composition root. The root separates
/// it into each stage's options view. Start from <see cref="Default"/> and adjust it with a
/// <c>with</c> expression.
/// </summary>
public sealed record CompilerOptions
{
    /// <summary>
    /// Speakers supplied by configuration — a registry seeded alongside a script's own speakers.
    /// At most one may be the default speaker, and a script's own <c>##default</c> takes
    /// precedence over it.
    /// </summary>
    public IReadOnlyList<ConfiguredSpeaker> Speakers { get; init; } = [];

    /// <summary>How far a compile proceeds after an error; the default is
    /// <see cref="CompilationMode.StageBoundary"/>.</summary>
    public CompilationMode Mode { get; init; } = CompilationMode.StageBoundary;

    /// <summary>
    /// How the front-end handles each kind of unmodeled Markdown construct, overriding the
    /// built-in defaults. Only the kinds named here differ; every other kind keeps its default,
    /// so an empty map (the default) is the built-in behavior.
    /// </summary>
    public IReadOnlyDictionary<UnmodeledNodeKind, UnmodeledNodeHandling> UnmodeledMarkdown
    {
        get;
        init;
    } = new Dictionary<UnmodeledNodeKind, UnmodeledNodeHandling>();

    /// <summary>The unconfigured options: every knob at its built-in default.</summary>
    public static CompilerOptions Default { get; } = new();

    /// <summary>Separates out the options the semantic analysis stage reads from the umbrella.</summary>
    internal ISemanticAnalyzerOptions ForSemanticAnalyzer() => new SemanticAnalyzerOptions(Speakers);
}

